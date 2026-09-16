using Invoyz.InvoiceService.Application;
using Invoyz.InvoiceService.Infra;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.BootstrapApplicationService();
builder.Services.ConfigureSqliteAsDatabaseEngine(builder.Configuration);

var host = builder.Build();
host.Run();
