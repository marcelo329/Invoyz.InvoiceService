using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Queries.Models;

public record GetInvoiceLinesQuery(Guid InvoiceId, int Page, int PageSize) : IRequest<IReadOnlyCollection<InvoiceLineContract>>;
