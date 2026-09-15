namespace Invoyz.InvoiceService.Contracts.InboundContracts.Products;

public record CreateProductContract(
    string Name,
    string Description,
    decimal UnitPrice,
    decimal TaxRate);
