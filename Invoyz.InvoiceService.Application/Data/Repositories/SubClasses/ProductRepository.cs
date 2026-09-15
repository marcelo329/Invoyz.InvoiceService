using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;

public sealed class ProductRepository : BaseRepository<ProductEntity>, IProductRepository
{
    public ProductRepository(AppDbContext appDbContext) : base(appDbContext) { }
}

