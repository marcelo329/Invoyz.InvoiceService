using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal UnitPrice,
    decimal TaxRate
) : IRequest<ErrorOr<Guid>>;
