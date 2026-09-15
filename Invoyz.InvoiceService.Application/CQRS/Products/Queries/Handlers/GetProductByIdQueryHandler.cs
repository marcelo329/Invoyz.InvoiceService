using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Products.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Queries.Handlers;

public sealed class GetProductByIdQueryHandler(IProductRepository productRepository) : IRequestHandler<GetProductByIdQuery, ErrorOr<ProductContract>>
{
    public async Task<ErrorOr<ProductContract>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await productRepository.GetById(request.Id, cancellationToken);

        if (result == null)
            return Error.NotFound($"Product with id {request.Id} not found.");

        return result.ToProductContract();
    }
}
