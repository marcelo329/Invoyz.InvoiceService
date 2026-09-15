using Invoyz.InvoiceService.Contracts.InboundContracts.InvoiceLines;

namespace Invoyz.InvoiceService.Contracts.InboundContracts.Invoices;

public record CreateInvoiceContract(
    string InvoiceNumber,
    Guid CustomerId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Status,
    IReadOnlyCollection<CreateInvoiceLineContract> Lines);
