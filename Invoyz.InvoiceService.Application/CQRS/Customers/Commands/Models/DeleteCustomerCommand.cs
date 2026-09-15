using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;

public record DeleteCustomerCommand(Guid Id) : BaseCQRSWithId(Id), IRequest<Error?>;
