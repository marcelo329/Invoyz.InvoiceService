namespace Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.InvoiceLines;

// UnitPrice and TaxRate are optional: when omitted they are snapshotted from the
// product, so a line records the price that applied when it was raised.
public record CreateInvoiceLineContract(
    Guid ProductId,
    int Quantity,
    decimal? UnitPrice = null,
    decimal? TaxRate = null);
