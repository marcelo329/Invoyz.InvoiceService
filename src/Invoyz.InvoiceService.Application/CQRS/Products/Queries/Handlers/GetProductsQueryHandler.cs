using Invoyz.InvoiceService.Application.CQRS.Products.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using MediatR;
using System.Collections.Immutable;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Queries.Handlers;

public sealed class GetProductsQueryHandler(IProductRepository productRepository) : IRequestHandler<GetProductsQuery, IReadOnlyCollection<ProductContract>>
{
    public async Task<IReadOnlyCollection<ProductContract>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var result = await productRepository.GetListAsync(request.Page, request.PageSize, cancellationToken);

        return result
            .Select(a => a.ToProductContract())
            .OrderBy(a => a.Name)
            .ToImmutableArray();
    }
}
