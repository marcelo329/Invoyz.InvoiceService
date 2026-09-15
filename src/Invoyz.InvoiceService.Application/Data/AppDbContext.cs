using Invoyz.InvoiceService.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoyz.InvoiceService.Application.Data;

public class AppDbContext(DbContextOptions<AppDbContext> dbContext) : DbContext(dbContext)
{
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();   
    public DbSet<InvoiceLineEntity> InvoiceLines => Set<InvoiceLineEntity>();   
    public DbSet<ProductEntity> Products => Set<ProductEntity>();   
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Filtered so uniqueness matches what the handlers enforce: a soft-deleted
        // invoice must not reserve its number forever.
        modelBuilder.Entity<InvoiceEntity>()
            .HasIndex(invoice => invoice.InvoiceNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = 0");

        modelBuilder.Entity<InvoiceLineEntity>()
            .HasIndex(line => line.InvoiceId);

        modelBuilder.Entity<InvoiceLineEntity>()
            .HasIndex(line => line.ProductId);
    }
}
