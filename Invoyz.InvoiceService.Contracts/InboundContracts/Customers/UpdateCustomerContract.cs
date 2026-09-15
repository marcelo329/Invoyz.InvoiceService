namespace Invoyz.InvoiceService.Contracts.InboundContracts.Customers;

public record UpdateCustomerContract(
    Guid Id,
    string Name,
    string Address,
    string Email,
    string VatNumber)
    : CreateCustomerContract(
        Name: Name,
        Address: Address,
        Email: Email,
        VatNumber: VatNumber);
