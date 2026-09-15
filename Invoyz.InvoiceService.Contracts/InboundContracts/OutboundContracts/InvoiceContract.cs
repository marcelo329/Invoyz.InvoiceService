namespace Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;

public record InvoiceContract(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Status,
    decimal SubTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    IReadOnlyCollection<InvoiceLineContract> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt
    );
