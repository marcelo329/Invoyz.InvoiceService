namespace Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Invoices;

public record UpdateInvoiceContract(
    string InvoiceNumber,
    Guid CustomerId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Status);
