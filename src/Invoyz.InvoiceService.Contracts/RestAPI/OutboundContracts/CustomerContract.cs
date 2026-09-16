namespace Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;

public record CustomerContract(
    Guid Id,
    string Name,
    string Address,
    string Email,
    string VatNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt
    );
