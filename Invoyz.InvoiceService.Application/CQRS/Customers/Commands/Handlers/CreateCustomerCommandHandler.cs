using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Handlers;

public sealed class CreateCustomerCommandHandler(
	ICustomerRepository _customerRepository,
	ILogger<CreateCustomerCommandHandler> _logger) : IRequestHandler<CreateCustomerCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Customer creation process.");
        var msg = "Failed to create customer";

        try
        {
            var vatNumberInUse = await _customerRepository.GetByVatNumberAsync(request.VatNumber, cancellationToken);

            if (vatNumberInUse != null)
                return Error.Conflict($"Vat number already in use on customer with id {vatNumberInUse.Id}");

            var result = await _customerRepository.CreateAsync(new Domains.Entities.CustomerEntity
            {
                Address = request.Address,
                Email = request.Email,
                Name = request.Name,
                VatNumber = request.VatNumber
            }, cancellationToken);

            if (result.Entity.Id == default)
            {
                _logger.LogError(msg);
                return Error.Failure(msg);
            }

            _logger.LogInformation("Customer created successfully with Id {customerId}.", result.Entity.Id);

            return result.Entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, msg);
            return Error.Failure($"{msg}.Error:" + ex.Message);
        }
    }
}
