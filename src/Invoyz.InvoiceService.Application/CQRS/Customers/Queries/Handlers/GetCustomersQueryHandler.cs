using Invoyz.InvoiceService.Application.CQRS.Customers.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using MediatR;
using System.Collections.Immutable;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Queries.Handlers;

public sealed class GetCustomersQueryHandler(ICustomerRepository customerRepository) : IRequestHandler<GetCustomersQuery, IReadOnlyCollection<CustomerContract>>
{
    public async Task<IReadOnlyCollection<CustomerContract>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var result = await customerRepository.GetListAsync(request.Page, request.PageSize, cancellationToken);

        return result
            .Select(a => a.ToCustomerContract())
            .OrderBy(a => a.Name)
            .ToImmutableArray();
    }
}
