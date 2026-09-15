using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Handlers;

public sealed class DeleteInvoiceCommandHandler(
    IInvoiceRepository _invoiceRepository,
    ILogger<DeleteInvoiceCommandHandler> _logger) : IRequestHandler<DeleteInvoiceCommand, Error?>
{
    public async Task<Error?> Handle(DeleteInvoiceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start invoice {invoiceId} deletion process.", request.Id);

        try
        {
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(request.Id, cancellationToken);

            if (invoice == null)
                return Error.NotFound($"Invoice with id {request.Id} not found.");

            // Lines have no meaning without their invoice, so the soft delete cascades:
            // leaving them live would keep them blocking product deletion forever.
            foreach (var line in invoice.InvoiceLines.Where(line => !line.IsDeleted))
            {
                line.IsDeleted = true;
                line.DeletedAt = DateTimeOffset.UtcNow;
            }

            var deleted = await _invoiceRepository.DeleteAsync(invoice, cancellationToken);

            if (!deleted)
            {
                _logger.LogWarning("Failed to delete invoice {invoiceId}.", request.Id);
                return Error.Failure($"Failed to delete invoice with id {request.Id}.");
            }

            _logger.LogInformation("Invoice {invoiceId} deleted successfully.", request.Id);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deletion process for invoice {invoiceId}.", request.Id);
            return Error.Failure($"Failed to delete invoice with id {request.Id}.Error:" + ex.Message);
        }
    }
}
