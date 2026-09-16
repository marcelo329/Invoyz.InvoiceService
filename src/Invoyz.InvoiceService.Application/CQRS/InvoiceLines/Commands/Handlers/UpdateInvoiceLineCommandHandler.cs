using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.EventServices.Events;
using Invoyz.InvoiceService.Application.Helpers;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Handlers;

public sealed class UpdateInvoiceLineCommandHandler(
    IInvoiceRepository _invoiceRepository,
    IProductRepository _productRepository,
    IPublishEndpoint _publishEndpoint,
    ILogger<UpdateInvoiceLineCommandHandler> _logger) : IRequestHandler<UpdateInvoiceLineCommand, Error?>
{
    public async Task<Error?> Handle(UpdateInvoiceLineCommand request, CancellationToken cancellationToken)
    {
        var msg = "Failed to update invoice line";

        _logger.LogInformation("Starting invoice line {lineId} update.", request.Id);

        try
        {
            var invoice = await _invoiceRepository.GetByIdWithLinesAsync(request.InvoiceId, cancellationToken);

            if (invoice == null)
                return Error.NotFound($"Invoice with id {request.InvoiceId} not found.");

            // Scoped lookup: a line belonging to a different invoice must read as missing.
            var line = invoice.InvoiceLines
                .FirstOrDefault(l => l.Id == request.Id && !l.IsDeleted);

            if (line == null)
                return Error.NotFound($"Invoice line with id {request.Id} not found on invoice {request.InvoiceId}.");

            var product = await _productRepository.GetById(request.ProductId, cancellationToken);

            if (product == null)
                return Error.NotFound($"Product with id {request.ProductId} not found.");

            var unitPrice = request.UnitPrice ?? product.UnitPrice;
            var taxRate = request.TaxRate ?? product.TaxRate;

            var sameProduct = request.ProductId == line.ProductId;
            var sameQuantity = request.Quantity == line.Quantity;
            var sameUnitPrice = unitPrice == line.UnitPrice;
            var sameTaxRate = taxRate == line.TaxRate;

            if (sameProduct && sameQuantity && sameUnitPrice && sameTaxRate)
                return Error.Conflict("No changes detected. No changes applied.");

            line.ProductId = request.ProductId;
            line.Quantity = request.Quantity;
            line.UnitPrice = unitPrice;
            line.TaxRate = taxRate;
            line.LastModifiedAt = DateTimeOffset.UtcNow;

            InvoiceTotals.ApplyLineTotals(line);
            InvoiceTotals.Recalculate(invoice);

            var errorUpdating = await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

            if (errorUpdating != null)
            {
                _logger.LogError("Error updating invoice line.Error:" + errorUpdating.Value.Code);
                return errorUpdating;
            }

            await _publishEndpoint.Publish(new InvoiceUpdated(request.InvoiceId));
            _logger.LogInformation("{EventName} published", nameof(InvoiceUpdated));

            _logger.LogInformation("Invoice line {lineId} updated successfully.", request.Id);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, msg);
            return Error.Failure($"{msg}.Error:" + ex.Message);
        }
    }
}
