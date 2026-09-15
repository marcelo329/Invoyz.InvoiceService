using Invoyz.InvoiceService.Application.Data;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;
using Invoyz.InvoiceService.Infra;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceLineRepository, InvoiceLineRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.ConfigureSqliteAsDatabaseEngine(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
