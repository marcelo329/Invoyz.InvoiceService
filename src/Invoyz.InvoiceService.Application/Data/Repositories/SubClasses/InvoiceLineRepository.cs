using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;

public sealed class InvoiceLineRepository : BaseRepository<InvoiceLineEntity>, IInvoiceLineRepository
{
    public InvoiceLineRepository(AppDbContext appDbContext) : base(appDbContext) { }

    // Scoped by invoice: a line id that belongs to another invoice must read as missing,
    // not as somebody else data.
    public Task<InvoiceLineEntity?> GetByIdForInvoiceAsync(Guid invoiceId, Guid lineId, CancellationToken cancellationToken)
        => base.appDbContext
        .Set<InvoiceLineEntity>()
        .FirstOrDefaultAsync(
            line => line.Id == lineId && line.InvoiceId == invoiceId && !line.IsDeleted,
            cancellationToken);

    public async Task<IReadOnlyCollection<InvoiceLineEntity>> GetListForInvoiceAsync(Guid invoiceId, int page, int totalRows, CancellationToken cancellationToken)
        => await base.appDbContext
        .Set<InvoiceLineEntity>()
        .AsNoTracking()
        .Where(line => line.InvoiceId == invoiceId && !line.IsDeleted)
        .Skip((page - 1) * totalRows)
        .Take(totalRows)
        .ToListAsync(cancellationToken);
}
