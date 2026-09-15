namespace Invoyz.InvoiceService.Contracts.InboundContracts.Invoices;

public record UpdateInvoiceContract(
    string InvoiceNumber,
    Guid CustomerId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Status);
