using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;

public record DeleteInvoiceCommand(Guid Id) : BaseCQRSWithId(Id), IRequest<Error?>;
