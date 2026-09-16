namespace Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;

public record ProductContract(
    Guid Id,
    string Name,
    string Description,
    decimal UnitPrice,
    decimal TaxRate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt
    );
