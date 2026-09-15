namespace Invoyz.InvoiceService.Contracts.InboundContracts.Customers;

public record CreateCustomerContract(string Name, string Address, string Email, string VatNumber);