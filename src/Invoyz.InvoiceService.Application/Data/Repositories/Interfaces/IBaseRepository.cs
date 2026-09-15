using ErrorOr;
using Invoyz.InvoiceService.Domains.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;

public interface IBaseRepository<T> where T : class
{
    ValueTask<EntityEntry<T>> CreateAsync(T entity, CancellationToken cancellationToken);
    Task<Error?> UpdateAsync(BaseEntity entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(BaseEntity entity, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<T>> GetListAsync(int page, int totalRows, CancellationToken cancellationToken);
    Task<T?> GetById(Guid Id, CancellationToken cancellationToken);
}
