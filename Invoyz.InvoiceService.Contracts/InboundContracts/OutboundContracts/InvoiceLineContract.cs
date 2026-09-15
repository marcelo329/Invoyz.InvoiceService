namespace Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;

public record InvoiceLineContract(
    Guid Id,
    Guid InvoiceId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal LineTotal,
    decimal LineTax,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt
    );
