namespace Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Customers;

public record CreateCustomerContract(string Name, string Address, string Email, string VatNumber);