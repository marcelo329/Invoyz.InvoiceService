using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Validators;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;
using Invoyz.InvoiceService.Application.EventServices.Consumer.EventConsumers;
using Invoyz.InvoiceService.Application.PdfGenerator.Interfaces;
using Invoyz.InvoiceService.Application.PdfGenerator.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Invoyz.InvoiceService.Application;

public static class Bootstrapper
{
    public static IServiceCollection BootstrapApplicationService(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateCustomerCommandValidator>();
        services.AddValidatorsFromAssemblyContaining<UpdateCustomerCommandValidator>();

        services.AddScoped<IPdfGenerator, InvoicePdfCreationWithQuestPdfService>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceLineRepository, InvoiceLineRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<InvoiceStatusConsumer>();

            x.UsingInMemory((context, cfg) => //using inMemory to avoid the bootstrap of a third party broker service like event hub or Kafka. Is plug&play.
            {
                cfg.ConfigureEndpoints(context);
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(1)));
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                    cb.TripThreshold = 15;
                    cb.ActiveThreshold = 10;
                    cb.ResetInterval = TimeSpan.FromMinutes(5);
                });
            }
            );
        });

        return services;
    }
}
