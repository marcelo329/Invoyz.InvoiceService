using ErrorOr;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;

public record GetInvoiceByIdQuery(Guid Id) : BaseCQRSWithId(Id), IRequest<ErrorOr<InvoiceContract>>;
