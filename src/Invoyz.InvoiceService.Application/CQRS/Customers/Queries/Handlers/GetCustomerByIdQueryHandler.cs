using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Customers.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Queries.Handlers;

public sealed class GetCustomerByIdQueryHandler(ICustomerRepository customerRepository) : IRequestHandler<GetCustomerByIdQuery, ErrorOr<CustomerContract>>
{
    public async Task<ErrorOr<CustomerContract>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await customerRepository.GetById(request.Id, cancellationToken);

        if(result == null)
            return Error.NotFound($"Customer with id {request.Id} not found.");

        return result.ToCustomerContract();
    }
}
