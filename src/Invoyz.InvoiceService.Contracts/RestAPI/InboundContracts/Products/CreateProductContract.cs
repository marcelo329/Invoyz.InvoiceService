namespace Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Products;

public record CreateProductContract(
    string Name,
    string Description,
    decimal UnitPrice,
    decimal TaxRate);
