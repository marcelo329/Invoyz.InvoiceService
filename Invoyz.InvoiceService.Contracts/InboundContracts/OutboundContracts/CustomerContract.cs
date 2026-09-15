namespace Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;

public record CustomerContract(
    string Name, 
    string Address, 
    string Email,
    string VatNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastModifiedAt
    );
