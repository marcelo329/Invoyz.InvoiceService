using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;

public record GetInvoicesQuery(int Page, int PageSize) : IRequest<IReadOnlyCollection<InvoiceContract>>;
