using ErrorOr;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;

public sealed class CustomerRepository : BaseRepository<CustomerEntity>, ICustomerRepository
{
    public CustomerRepository(AppDbContext appDbContext) : base(appDbContext){}
}
