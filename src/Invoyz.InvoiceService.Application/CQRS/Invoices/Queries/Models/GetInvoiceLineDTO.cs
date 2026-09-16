namespace Invoyz.InvoiceService.InvoiceWorker.InvoiceGeneration.Models;

public record GetInvoiceLineDTO(string ProductName, string Description, decimal UnitPrice, decimal TaxRate, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
    public decimal LineTax => LineTotal * TaxRate / 100;
    public decimal LineGrandTotal => LineTotal + LineTax;
}