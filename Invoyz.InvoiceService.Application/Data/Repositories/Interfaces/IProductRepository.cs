using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;

public interface IProductRepository : IBaseRepository<ProductEntity>
{
    Task<bool> IsReferencedByInvoiceLineAsync(Guid productId, CancellationToken cancellationToken);
}
