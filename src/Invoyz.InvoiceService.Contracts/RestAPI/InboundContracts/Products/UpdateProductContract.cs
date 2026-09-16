namespace Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Products;

public record UpdateProductContract(
    string Name,
    string Description,
    decimal UnitPrice,
    decimal TaxRate);
