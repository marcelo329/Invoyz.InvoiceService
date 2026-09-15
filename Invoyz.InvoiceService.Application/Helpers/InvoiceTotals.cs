using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Helpers;

/// <summary>
/// Line and invoice money is always derived here, never accepted from a payload.
/// Every mutation of a line must end with <see cref="Recalculate"/> on its invoice,
/// otherwise the stored rollup drifts away from the lines it is meant to summarise.
/// </summary>
public static class InvoiceTotals
{
    public static void ApplyLineTotals(InvoiceLineEntity line)
    {
        line.LineTotal = decimal.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
        line.LineTax = decimal.Round(line.LineTotal * line.TaxRate, 2, MidpointRounding.AwayFromZero);
    }

    public static void Recalculate(InvoiceEntity invoice)
    {
        var liveLines = invoice.InvoiceLines?.Where(line => !line.IsDeleted) ?? [];

        invoice.SubTotal = decimal.Round(liveLines.Sum(line => line.LineTotal), 2, MidpointRounding.AwayFromZero);
        invoice.TaxTotal = decimal.Round(liveLines.Sum(line => line.LineTax), 2, MidpointRounding.AwayFromZero);
        invoice.GrandTotal = invoice.SubTotal + invoice.TaxTotal;
    }
}
