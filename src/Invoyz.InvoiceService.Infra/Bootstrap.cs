using Invoyz.InvoiceService.Application.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Invoyz.InvoiceService.Infra;

public static class Bootstrap
{
    public static IServiceCollection ConfigureSqliteAsDatabaseEngine(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite(
            configuration.GetConnectionString("Sqlite"),
            sqlite => sqlite.MigrationsAssembly("Invoyz.InvoiceService.Infra")));
        return services;
    }
}
