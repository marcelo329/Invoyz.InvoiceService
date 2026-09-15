using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;

public sealed class ProductRepository : BaseRepository<ProductEntity>, IProductRepository
{
    public ProductRepository(AppDbContext appDbContext) : base(appDbContext) { }

    public Task<bool> IsReferencedByInvoiceLineAsync(Guid productId, CancellationToken cancellationToken)
        => base.appDbContext
        .Set<InvoiceLineEntity>()
        .AsNoTracking()
        .AnyAsync(line => line.ProductId == productId && !line.IsDeleted, cancellationToken);
}
