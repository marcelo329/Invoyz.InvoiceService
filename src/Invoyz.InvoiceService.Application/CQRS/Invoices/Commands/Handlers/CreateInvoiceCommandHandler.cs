using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Domains.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Handlers;

public sealed class CreateInvoiceCommandHandler(
    IInvoiceRepository _invoiceRepository,
    ICustomerRepository _customerRepository,
    IProductRepository _productRepository,
    ILogger<CreateInvoiceCommandHandler> _logger) : IRequestHandler<CreateInvoiceCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Invoice creation process.");
        var msg = "Failed to create invoice";

        try
        {
            var invoiceNumberInUse = await _invoiceRepository.GetByInvoiceNumberAsync(request.InvoiceNumber, cancellationToken);

            if (invoiceNumberInUse != null)
                return Error.Conflict($"Invoice number already in use on invoice with id {invoiceNumberInUse.Id}");

            var customer = await _customerRepository.GetById(request.CustomerId, cancellationToken);

            if (customer == null)
                return Error.NotFound($"Customer with id {request.CustomerId} not found.");

            var invoice = new InvoiceEntity
            {
                InvoiceNumber = request.InvoiceNumber,
                CustomerId = request.CustomerId,
                IssueDate = request.IssueDate,
                DueDate = request.DueDate,
                Status = InvoiceStatuses.Normalise(request.Status),
                InvoiceLines = []
            };

            foreach (var requestedLine in request.Lines)
            {
                var product = await _productRepository.GetById(requestedLine.ProductId, cancellationToken);

                if (product == null)
                    return Error.NotFound($"Product with id {requestedLine.ProductId} not found.");

                var line = new InvoiceLineEntity
                {
                    ProductId = product.Id,
                    Quantity = requestedLine.Quantity,
                    // Absent price or rate means "whatever the product costs today".
                    UnitPrice = requestedLine.UnitPrice ?? product.UnitPrice,
                    TaxRate = requestedLine.TaxRate ?? product.TaxRate,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                InvoiceTotals.ApplyLineTotals(line);
                invoice.InvoiceLines.Add(line);
            }

            InvoiceTotals.Recalculate(invoice);

            var result = await _invoiceRepository.CreateAsync(invoice, cancellationToken);

            _logger.LogInformation("Invoice created successfully with Id {invoiceId}.", result.Entity.Id);

            return result.Entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, msg);
            return Error.Failure($"{msg}.Error:" + ex.Message);
        }
    }
}
