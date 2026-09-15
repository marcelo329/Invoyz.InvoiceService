using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;

public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string Address,
    string Email,
    string VatNumber) : CreateCustomerCommand(
        Name: Name,
        Address: Address,
        Email: Email,
        VatNumber: VatNumber), IRequest<Error?>;
