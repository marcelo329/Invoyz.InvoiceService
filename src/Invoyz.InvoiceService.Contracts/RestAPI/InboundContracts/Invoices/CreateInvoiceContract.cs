using Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.InvoiceLines;

namespace Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Invoices;

public record CreateInvoiceContract(
    string InvoiceNumber,
    Guid CustomerId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Status,
    IReadOnlyCollection<CreateInvoiceLineContract> Lines);
