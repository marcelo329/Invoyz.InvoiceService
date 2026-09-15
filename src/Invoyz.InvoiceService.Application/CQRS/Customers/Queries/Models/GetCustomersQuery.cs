using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Queries.Models;

public record GetCustomersQuery(int Page, int PageSize) : IRequest<IReadOnlyCollection<CustomerContract>>;