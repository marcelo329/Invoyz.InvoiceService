using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;
using Invoyz.InvoiceService.Contracts.InboundContracts.Customers;

namespace Invoyz.InvoiceService.Extensions;

public static class Mappers
{
    public static CreateCustomerCommand MapToCreateCustomerCommand(this CreateCustomerContract contract)
        => new CreateCustomerCommand(
            Name: contract.Name,
            Email: contract.Email,
            Address: contract.Address,
            VatNumber: contract.VatNumber
        );

    public static UpdateCustomerCommand MapToUpdateCustomerCommand(this UpdateCustomerContract contract, Guid id)
        => new UpdateCustomerCommand(
            Id: contract.Id,
            Name: contract.Name,
            Email: contract.Email,
            Address: contract.Address,
            VatNumber: contract.VatNumber
        );
}
