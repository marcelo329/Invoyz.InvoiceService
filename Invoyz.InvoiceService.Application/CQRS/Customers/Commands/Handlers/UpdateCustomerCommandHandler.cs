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
        _logger.LogInformation("Starting Customer update process.");


    }
}
