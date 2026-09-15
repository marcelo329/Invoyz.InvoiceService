using ErrorOr;
using Invoyz.InvoiceService.Domains.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Invoyz.InvoiceService.Application.Data.Repositories;

public abstract class BaseRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly AppDbContext appDbContext;

    protected BaseRepository(AppDbContext appDbContext)
    {
        this.appDbContext = appDbContext;
    }

    public ValueTask<EntityEntry<TEntity>> CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        entity.CreatedAt = DateTimeOffset.UtcNow;
        return appDbContext.AddAsync(entity, cancellationToken);
    }

    public async Task<TEntity?> GetById(Guid Id, CancellationToken cancellationToken)
        => await appDbContext
        .Set<TEntity>()
        .FirstOrDefaultAsync(a => a.Id.Equals(Id), cancellationToken);

    public async Task<IReadOnlyCollection<TEntity>> GetListAsync(int page, int totalRows, CancellationToken cancellationToken)
        => await appDbContext
        .Set<TEntity>()
        .AsNoTracking()
        .Skip((page -1) * totalRows)
        .Take(totalRows)
        .ToListAsync(cancellationToken);

    public async Task<Error?> UpdateAsync(BaseEntity entity, CancellationToken cancellationToken)
    {
        entity.LastModifiedAt = DateTimeOffset.UtcNow;
        var operationResult = await appDbContext.SaveChangesAsync(cancellationToken);

        if (operationResult == 0)
            return Error.Failure("Model not updated");

        return null;
    }

    public async Task<bool> DeleteAsync(BaseEntity entity, CancellationToken cancellationToken)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        return (await this.appDbContext.SaveChangesAsync(cancellationToken)) > 0;
    }
}
