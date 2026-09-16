using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Queries.Models;

public record GetProductsQuery(int Page, int PageSize) : IRequest<IReadOnlyCollection<ProductContract>>;
