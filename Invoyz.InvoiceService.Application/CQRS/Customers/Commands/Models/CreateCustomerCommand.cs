using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;

public record CreateCustomerCommand(
    string Name,
    string Email,
    string Address,
    string VatNumber
) : IRequest<ErrorOr<Guid>>;
