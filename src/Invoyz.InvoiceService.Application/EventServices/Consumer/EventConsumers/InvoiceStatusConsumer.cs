using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.EventServices.Events;
using Invoyz.InvoiceService.Application.PdfGenerator.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.EventServices.Consumer.EventConsumers;

public sealed class InvoiceStatusConsumer(
    ILogger<InvoiceStatusConsumer> _logger,
    IPdfGenerator pdfGenerator,
    IInvoiceRepository _invoiceRepository) : IConsumer<InvoiceUpdated>
{
    public async Task Consume(ConsumeContext<InvoiceUpdated> context)
    {
        var msg = context.Message;

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        cts.Token.Register(() => 
        {
            _logger.LogWarning("Cancellation token timeout called while consuming message with StateMachine:{StateMachine} - Invoice:{InvoiceId}", nameof(InvoiceUpdated), msg.InvoiceId);
        });

        _logger.LogInformation("StateMachine:{StateMachine} - Invoice:{InvoiceId}", nameof(InvoiceUpdated), msg.InvoiceId);

        var invoiceWithInclusion = await _invoiceRepository.GetEagerLoadingAsync(msg.InvoiceId,cts.Token);

        if(invoiceWithInclusion == null)
        {
            _logger.LogWarning("Invoice:{InvoiceId} not found on database. Requeuing...", msg.InvoiceId);
            throw new Exception($"Invoice:{msg.InvoiceId} not found on database. Requeuing...");
        }

        pdfGenerator.InvoicePdfCreation(invoiceWithInclusion, msg);
    }
}
