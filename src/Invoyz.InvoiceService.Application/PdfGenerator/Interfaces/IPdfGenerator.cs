using Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;
using Invoyz.InvoiceService.Application.EventServices.Events;

namespace Invoyz.InvoiceService.Application.PdfGenerator.Interfaces;

public interface IPdfGenerator
{
    void InvoicePdfCreation(GetInvoiceEagerLoadingDTO invoice, InvoiceUpdated msg);
}
