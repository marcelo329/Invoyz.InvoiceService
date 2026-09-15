using ErrorOr;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Queries.Models;

public record GetProductByIdQuery(Guid Id) : BaseCQRSWithId(Id), IRequest<ErrorOr<ProductContract>>;
