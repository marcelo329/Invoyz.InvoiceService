namespace Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Customers;

public record UpdateCustomerContract(
    string Name,
    string Address,
    string Email,
    string VatNumber);
