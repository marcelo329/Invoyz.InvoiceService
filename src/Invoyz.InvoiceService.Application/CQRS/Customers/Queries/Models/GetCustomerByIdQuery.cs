using ErrorOr;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Queries.Models;

public record GetCustomerByIdQuery(Guid Id) : BaseCQRSWithId(Id), IRequest<ErrorOr<CustomerContract>>;
