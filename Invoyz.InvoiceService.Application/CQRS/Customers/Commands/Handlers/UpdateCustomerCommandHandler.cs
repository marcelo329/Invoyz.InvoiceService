using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Handlers;

public sealed class UpdateCustomerCommandHandler(
    ICustomerRepository _customerRepository,
    ILogger<UpdateCustomerCommandHandler> _logger) : IRequestHandler<UpdateCustomerCommand, Error?>
{
    public async Task<Error?> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var msg = "Failed to update customer";

        _logger.LogInformation("Starting Customer update process.");

		try
		{
            var customer = await _customerRepository.GetById(request.Id, cancellationToken);

            if (customer == null)
                return Error.NotFound($"Customer with id {request.Id} not found.");

            var vatNumberInUse = await _customerRepository.GetByVatNumberAsync(request.VatNumber, cancellationToken);

            if(vatNumberInUse != null && vatNumberInUse.Id != request.Id)
                return Error.Conflict("Vat number already in use on customer with id {customerId}", vatNumberInUse.Id.ToString());

            var sameName = request.Name.Equals(customer.Name);
            var sameAddress = request.Address.Equals(customer.Address);
            var sameVatNumber = request.VatNumber.Equals(customer.VatNumber);
            var sameEmail = request.Email.Equals(customer.Email);

            if (sameName || sameAddress || sameVatNumber || sameEmail)
                return Error.Conflict("No changes detected. No changes applied.");

            customer.Address = request.Address;
            customer.Email = request.Email;
            customer.VatNumber = request.VatNumber;
            customer.Name = request.Name;

            var errorUploading = await _customerRepository.UpdateAsync(customer, cancellationToken);

            if(errorUploading != null)
            {
                _logger.LogError("Error uploading.Error:" + errorUploading.Value.Code);
                return errorUploading;
            }

            _logger.LogInformation("Customer with Id {customerId} updated successfully", request.Id);

            return null;
        }
        catch (Exception ex)
		{
            _logger.LogError(ex, msg);
            return Error.Failure($"{msg}.Error:" + ex.Message);
        }
    }
}
