using ErrorOr;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;

public sealed class CustomerRepository : BaseRepository<CustomerEntity>, ICustomerRepository
{
    public CustomerRepository(AppDbContext appDbContext) : base(appDbContext){}

    public Task<CustomerEntity?> GetByVatNumberAsync(string vatNumber, CancellationToken cancellationToken)
        => base.appDbContext
        .Set<CustomerEntity>()
        .AsNoTracking()
        .FirstOrDefaultAsync(a => a.VatNumber.ToLower().Equals(vatNumber.ToLower()) && !a.IsDeleted, cancellationToken);
}
