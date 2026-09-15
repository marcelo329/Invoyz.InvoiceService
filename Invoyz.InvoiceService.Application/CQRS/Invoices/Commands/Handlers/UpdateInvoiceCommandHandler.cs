using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Handlers;

public sealed class UpdateInvoiceCommandHandler(
    IInvoiceRepository _invoiceRepository,
    ICustomerRepository _customerRepository,
    ILogger<UpdateInvoiceCommandHandler> _logger) : IRequestHandler<UpdateInvoiceCommand, Error?>
{
    public async Task<Error?> Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var msg = "Failed to update invoice";

        _logger.LogInformation("Starting Invoice update process.");

        try
        {
            var invoice = await _invoiceRepository.GetById(request.Id, cancellationToken);

            if (invoice == null)
                return Error.NotFound($"Invoice with id {request.Id} not found.");

            var invoiceNumberInUse = await _invoiceRepository.GetByInvoiceNumberAsync(request.InvoiceNumber, cancellationToken);

            if (invoiceNumberInUse != null && invoiceNumberInUse.Id != request.Id)
                return Error.Conflict($"Invoice number already in use on invoice with id {invoiceNumberInUse.Id}");

            var customer = await _customerRepository.GetById(request.CustomerId, cancellationToken);

            if (customer == null)
                return Error.NotFound($"Customer with id {request.CustomerId} not found.");

            var status = InvoiceStatuses.Normalise(request.Status);

            var sameInvoiceNumber = request.InvoiceNumber.Equals(invoice.InvoiceNumber);
            var sameCustomer = request.CustomerId == invoice.CustomerId;
            var sameIssueDate = request.IssueDate == invoice.IssueDate;
            var sameDueDate = request.DueDate == invoice.DueDate;
            var sameStatus = status.Equals(invoice.Status);

            if (sameInvoiceNumber && sameCustomer && sameIssueDate && sameDueDate && sameStatus)
                return Error.Conflict("No changes detected. No changes applied.");

            invoice.InvoiceNumber = request.InvoiceNumber;
            invoice.CustomerId = request.CustomerId;
            invoice.IssueDate = request.IssueDate;
            invoice.DueDate = request.DueDate;
            invoice.Status = status;

            var errorUpdating = await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

            if (errorUpdating != null)
            {
                _logger.LogError("Error updating.Error:" + errorUpdating.Value.Code);
                return errorUpdating;
            }

            _logger.LogInformation("Invoice with Id {invoiceId} updated successfully", request.Id);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, msg);
            return Error.Failure($"{msg}.Error:" + ex.Message);
        }
    }
}
