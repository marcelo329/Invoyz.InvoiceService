using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;

public record UpdateInvoiceLineCommand(
    Guid Id,
    Guid InvoiceId,
    Guid ProductId,
    int Quantity,
    decimal? UnitPrice,
    decimal? TaxRate) : BaseCQRSWithId(Id), IRequest<Error?>;
