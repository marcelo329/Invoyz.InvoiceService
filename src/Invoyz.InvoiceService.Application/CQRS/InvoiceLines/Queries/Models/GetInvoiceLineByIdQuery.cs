using ErrorOr;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Queries.Models;

public record GetInvoiceLineByIdQuery(Guid Id, Guid InvoiceId) : BaseCQRSWithId(Id), IRequest<ErrorOr<InvoiceLineContract>>;
