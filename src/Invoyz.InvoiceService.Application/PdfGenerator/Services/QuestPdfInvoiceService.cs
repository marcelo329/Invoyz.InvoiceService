using Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Invoyz.InvoiceService.Application.PdfGenerator.Services;

public sealed class QuestPdfInvoiceService : IDocument
{
    private readonly GetInvoiceEagerLoadingDTO _invoice;

    public QuestPdfInvoiceService(GetInvoiceEagerLoadingDTO invoice) => _invoice = invoice;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));

            page.Content().Column(col =>
            {
                col.Spacing(15);

                col.Item().Element(ComposeCustomerHeader);

                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                col.Item().Element(ComposeInvoiceLines);

                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                col.Item().Element(ComposeInvoiceInfo);

                col.Item().Element(ComposeTotals);
            });
        });
    }

    private void ComposeCustomerHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem();

            row.RelativeItem().Column(col =>
            {
                col.Item().AlignRight().Text(_invoice.Name).Bold().FontSize(13);
                col.Item().AlignRight().Text(_invoice.Address).FontColor(Colors.Grey.Darken1);
                col.Item().AlignRight().Text(_invoice.Email).FontColor(Colors.Grey.Darken1);
                col.Item().AlignRight().Text(_invoice.VatNumber).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeInvoiceLines(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);   // Product name
                cols.RelativeColumn(3);   // Description
                cols.RelativeColumn(1);   // Unit price
                cols.RelativeColumn(1);   // Qty
                cols.RelativeColumn(1);   // Tax rate
                cols.RelativeColumn(1);   // Line total
                cols.RelativeColumn(1);   // Line tax
            });

            table.Header(header =>
            {
                header.Cell().Text("Product").Bold();
                header.Cell().Text("Description").Bold();
                header.Cell().Text("Unit price").Bold();
                header.Cell().Text("Qty").Bold();
                header.Cell().Text("Tax rate").Bold();
                header.Cell().Text("Line total").Bold();
                header.Cell().Text("Line tax").Bold();

                header.Cell().ColumnSpan(7).PaddingTop(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
            });

            foreach (var line in _invoice.InvoiceLines)
            {
                table.Cell().Text(line.ProductName);
                table.Cell().Text(line.Description).FontColor(Colors.Grey.Darken1);
                table.Cell().Text(line.UnitPrice.ToString("C"));
                table.Cell().Text(line.Quantity.ToString());
                table.Cell().Text($"{line.TaxRate}%");
                table.Cell().Text(line.LineTotal.ToString("C"));
                table.Cell().Text(line.LineTax.ToString("C"));
            }
        });
    }

    private void ComposeInvoiceInfo(IContainer container)
    {
        container.Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Issue date").FontColor(Colors.Grey.Darken1).FontSize(9);
                col.Item().Text(_invoice.IssueDate.ToString("dd MMM yyyy")).Bold();
            });

            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Due date").FontColor(Colors.Grey.Darken1).FontSize(9);
                col.Item().Text(_invoice.DueDate.ToString("dd MMM yyyy")).Bold();
            });

            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Status").FontColor(Colors.Grey.Darken1).FontSize(9);
                col.Item().Text(_invoice.Status).Bold();
            });
        });
    }

    private void ComposeTotals(IContainer container)
    {
        container.AlignRight().Width(200).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotal");
                row.RelativeItem().AlignRight().Text(_invoice.Subtotal.ToString("C"));
            });

            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Total tax");
                row.RelativeItem().AlignRight().Text(_invoice.TotalTax.ToString("C"));
            });

            col.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Darken1);

            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Grand total").Bold().FontSize(12);
                row.RelativeItem().AlignRight().Text(_invoice.GrandTotal.ToString("C")).Bold().FontSize(12);
            });
        });
    }
}