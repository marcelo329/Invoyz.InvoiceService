using Invoyz.InvoiceService.InvoiceWorker.InvoiceGeneration.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;

public record GetInvoiceEagerLoadingDTO(
    string Name, 
    string Address,
    string Email, 
    string VatNumber,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Status,
    GetInvoiceLineDTO[] InvoiceLines)
{
    public decimal Subtotal => InvoiceLines.Sum(l => l.LineTotal);
    public decimal TotalTax => InvoiceLines.Sum(l => l.LineTax);
    public decimal GrandTotal => Subtotal + TotalTax;
}
