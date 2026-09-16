using Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;
using Invoyz.InvoiceService.InvoiceWorker.InvoiceGeneration.Models;
using Microsoft.EntityFrameworkCore;

namespace Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;

public sealed class InvoiceRepository : BaseRepository<InvoiceEntity>, IInvoiceRepository
{
    public InvoiceRepository(AppDbContext appDbContext) : base(appDbContext) { }

    public Task<InvoiceEntity?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken)
        => base.appDbContext
        .Set<InvoiceEntity>()
        .AsNoTracking()
        .FirstOrDefaultAsync(
            invoice => invoice.InvoiceNumber.ToLower().Equals(invoiceNumber.ToLower()) && !invoice.IsDeleted,
            cancellationToken);

    // Tracked on purpose: callers mutate the lines and then recalculate the rollup.
    public Task<InvoiceEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken)
        => base.appDbContext
        .Set<InvoiceEntity>()
        .Include(invoice => invoice.InvoiceLines)
        .FirstOrDefaultAsync(invoice => invoice.Id == id && !invoice.IsDeleted, cancellationToken);

    public async Task<IReadOnlyCollection<InvoiceEntity>> GetListWithLinesAsync(int page, int totalRows, CancellationToken cancellationToken)
        => await base.appDbContext
        .Set<InvoiceEntity>()
        .AsNoTracking()
        .Include(invoice => invoice.InvoiceLines.Where(line => !line.IsDeleted))
        .Where(invoice => !invoice.IsDeleted)
        .Skip((page - 1) * totalRows)
        .Take(totalRows)
        .ToListAsync(cancellationToken);

    public async Task<GetInvoiceEagerLoadingDTO?> GetEagerLoadingAsync(Guid Id, CancellationToken cancellationToken)
    {
        var result = await base.appDbContext
                .Set<InvoiceEntity>()
                .Include(c => c.Customer)
                .Include(il => il.InvoiceLines)
                    .ThenInclude(p => p.Product)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.Id.Equals(Id) && !a.IsDeleted, cancellationToken);

        if (result == null)
            return null;

        return new GetInvoiceEagerLoadingDTO(
            Name: result.Customer.Name,
            Address: result.Customer.Address,
            Email: result.Customer.Email,
            VatNumber: result.Customer.VatNumber,
            IssueDate: result.IssueDate,
            DueDate: result.DueDate,
            Status: result.Status,
            InvoiceLines: result.InvoiceLines.Select(a => 
                new GetInvoiceLineDTO(
                    ProductName: a.Product.Name,
                    Description: a.Product.Description,
                    UnitPrice: a.Product.UnitPrice,
                    TaxRate: a.Product.TaxRate,
                    Quantity: a.Quantity))
                .ToArray()
            );
    }
}
