namespace Invoyz.InvoiceService.Contracts.InboundContracts.Products;

public record UpdateProductContract(
    string Name,
    string Description,
    decimal UnitPrice,
    decimal TaxRate);
