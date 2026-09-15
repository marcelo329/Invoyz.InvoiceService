using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Helpers;

public static class Mappers
{
    public static CustomerContract ToCustomerContract(this CustomerEntity customer)
        => new CustomerContract(
                Address: customer.Address,
                CreatedAt: customer.CreatedAt,
                Email: customer.Email,
                LastModifiedAt: customer.LastModifiedAt,
                Name: customer.Name,
                VatNumber: customer.VatNumber);
}
