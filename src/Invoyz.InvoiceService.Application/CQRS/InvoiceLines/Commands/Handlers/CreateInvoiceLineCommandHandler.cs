using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.EventServices.Events;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Domains.Entities;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Handlers;

public sealed class CreateInvoiceLineCommandHandler(
    IInvoiceRepository _invoiceRepository,
    IProductRepository _productRepository,
    IPublishEndpoint _publishEndpoint,
    ILogger<CreateInvoiceLineCommandHandler> _logger) : IRequestHandler<CreateInvoiceLineCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateInvoiceLineCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting invoice line creation on invoice {invoiceId}.", request.InvoiceId);
        var msg = "Failed to create invoice line";

        try
        {
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(request.InvoiceId, cancellationToken);

            if (invoice == null)
                return Error.NotFound($"Invoice with id {request.InvoiceId} not found.");

            var product = await _productRepository.GetById(request.ProductId, cancellationToken);

            if (product == null)
                return Error.NotFound($"Product with id {request.ProductId} not found.");

            var line = new InvoiceLineEntity
            {
                InvoiceId = invoice.Id,
                ProductId = product.Id,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice ?? product.UnitPrice,
                TaxRate = request.TaxRate ?? product.TaxRate,
                CreatedAt = DateTimeOffset.UtcNow
            };

            InvoiceTotals.ApplyLineTotals(line);

            invoice.InvoiceLines.Add(line);
            InvoiceTotals.Recalculate(invoice);

            // The line is attached to the tracked invoice, so saving the invoice
            // persists both the new line and the refreshed rollup in one write.
            var errorUpdating = await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

            if (errorUpdating != null)
            {
                _logger.LogError("Error creating invoice line.Error:" + errorUpdating.Value.Code);
                return Error.Failure(msg);
            }

            await _publishEndpoint.Publish(new InvoiceUpdated(request.InvoiceId));
            _logger.LogInformation("{EventName} published", nameof(InvoiceUpdated));

            _logger.LogInformation("Invoice line {lineId} created on invoice {invoiceId}.", line.Id, invoice.Id);

            return line.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, msg);
            return Error.Failure($"{msg}.Error:" + ex.Message);
        }
    }
}
