namespace Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.InvoiceLines;

public record UpdateInvoiceLineContract(
    Guid ProductId,
    int Quantity,
    decimal? UnitPrice = null,
    decimal? TaxRate = null);
