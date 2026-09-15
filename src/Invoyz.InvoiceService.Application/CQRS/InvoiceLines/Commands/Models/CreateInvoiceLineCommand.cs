using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;

public record CreateInvoiceLineCommand(
    Guid InvoiceId,
    Guid ProductId,
    int Quantity,
    decimal? UnitPrice,
    decimal? TaxRate
) : IRequest<ErrorOr<Guid>>;
