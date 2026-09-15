using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Handlers;

public sealed class DeleteCustomerCommandHandler(
    ICustomerRepository _customerRepository, 
    ILogger<DeleteCustomerCommandHandler> _logger) : IRequestHandler<DeleteCustomerCommand, Error?>
{
    public async Task<Error?> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start customer {customerId} deletion process.", request.Id);

        try
        {
            var customer = await _customerRepository.GetById(request.Id, cancellationToken);

            if (customer == null)
                return Error.NotFound($"Customer with id {request.Id} not found.");

            var deleted = await _customerRepository.DeleteAsync(customer, cancellationToken);

            if(!deleted)
            {
                _logger.LogWarning("Failed to delete customer {customerId}.", request.Id);
                return Error.Failure($"Failed to delete customer with id {request.Id}.");
            }

            _logger.LogInformation("Customer {customerId} deleted successfully.", request.Id);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deletion process for customer {customerId}.", request.Id);
            return Error.Failure($"Failed to delete customer with id {request.Id}.Error:" + ex.Message);
        }
    }
}
