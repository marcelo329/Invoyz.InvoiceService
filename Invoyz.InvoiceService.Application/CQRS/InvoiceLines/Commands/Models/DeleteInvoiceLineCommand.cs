using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;

public record DeleteInvoiceLineCommand(Guid Id, Guid InvoiceId) : BaseCQRSWithId(Id), IRequest<Error?>;
