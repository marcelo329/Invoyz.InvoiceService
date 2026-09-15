using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;

public record DeleteProductCommand(Guid Id) : BaseCQRSWithId(Id), IRequest<Error?>;
