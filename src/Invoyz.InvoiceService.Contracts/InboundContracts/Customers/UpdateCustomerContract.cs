namespace Invoyz.InvoiceService.Contracts.InboundContracts.Customers;

public record UpdateCustomerContract(
    string Name,
    string Address,
    string Email,
    string VatNumber);
