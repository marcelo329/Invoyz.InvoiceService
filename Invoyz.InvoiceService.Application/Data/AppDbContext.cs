using Invoyz.InvoiceService.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoyz.InvoiceService.Application.Data;

public class AppDbContext(DbContextOptions<AppDbContext> dbContext) : DbContext(dbContext)
{
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();   
    public DbSet<InvoiceLineEntity> InvoiceLines => Set<InvoiceLineEntity>();   
    public DbSet<ProductEntity> Products => Set<ProductEntity>();   
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
}
