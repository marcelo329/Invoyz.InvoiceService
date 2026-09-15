using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;

public interface ICustomerRepository : IBaseRepository<CustomerEntity>
{
    Task<CustomerEntity?> GetByVatNumberAsync(string vatNumber, CancellationToken cancellationToken);
}
