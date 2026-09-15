using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;
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
}
