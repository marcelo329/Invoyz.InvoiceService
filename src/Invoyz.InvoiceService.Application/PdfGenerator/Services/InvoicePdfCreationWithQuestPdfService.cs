using Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;
using Invoyz.InvoiceService.Application.EventServices.Events;
using Invoyz.InvoiceService.Application.PdfGenerator.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Invoyz.InvoiceService.Application.PdfGenerator.Services;

public class InvoicePdfCreationWithQuestPdfService : IPdfGenerator
{
    public void InvoicePdfCreation(GetInvoiceEagerLoadingDTO invoice, InvoiceUpdated msg)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = new QuestPdfInvoiceService(invoice);

        var basePath = Path.Combine(Environment.CurrentDirectory, $"Invoices");

        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        var filePath = Path.Combine(basePath, $"{msg.InvoiceId}.pdf");
        document.GeneratePdf(filePath);
    }
}
