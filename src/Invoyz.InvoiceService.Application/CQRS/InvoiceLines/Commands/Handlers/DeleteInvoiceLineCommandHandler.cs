using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.EventServices.Events;
using Invoyz.InvoiceService.Application.Helpers;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Handlers;

public sealed class DeleteInvoiceLineCommandHandler(
    IInvoiceRepository _invoiceRepository,
    IPublishEndpoint _publishEndpoint,
    ILogger<DeleteInvoiceLineCommandHandler> _logger) : IRequestHandler<DeleteInvoiceLineCommand, Error?>
{
    public async Task<Error?> Handle(DeleteInvoiceLineCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start invoice line {lineId} deletion process.", request.Id);

        try
        {
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(request.InvoiceId, cancellationToken);

            if (invoice == null)
                return Error.NotFound($"Invoice with id {request.InvoiceId} not found.");

            var line = invoice.InvoiceLines
                .FirstOrDefault(l => l.Id == request.Id && !l.IsDeleted);

            if (line == null)
                return Error.NotFound($"Invoice line with id {request.Id} not found on invoice {request.InvoiceId}.");

            // The spec requires an invoice to carry at least one line, so the last one
            // cannot be removed on its own: delete the invoice instead.
            var liveLineCount = invoice.InvoiceLines.Count(l => !l.IsDeleted);

            if (liveLineCount == 1)
                return Error.Conflict("An invoice requires at least one line. Delete the invoice instead.");

            line.IsDeleted = true;
            line.DeletedAt = DateTimeOffset.UtcNow;

            InvoiceTotals.Recalculate(invoice);

            var errorUpdating = await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

            if (errorUpdating != null)
            {
                _logger.LogWarning("Failed to delete invoice line {lineId}.", request.Id);
                return Error.Failure($"Failed to delete invoice line with id {request.Id}.");
            }

            await _publishEndpoint.Publish(new InvoiceUpdated(request.InvoiceId));
            _logger.LogInformation("{EventName} published", nameof(InvoiceUpdated));

            _logger.LogInformation("Invoice line {lineId} deleted successfully.", request.Id);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deletion process for invoice line {lineId}.", request.Id);
            return Error.Failure($"Failed to delete invoice line with id {request.Id}.Error:" + ex.Message);
        }
    }
}
