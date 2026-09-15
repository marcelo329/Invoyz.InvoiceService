using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal UnitPrice,
    decimal TaxRate) : BaseCQRSWithId(Id), IRequest<Error?>;
