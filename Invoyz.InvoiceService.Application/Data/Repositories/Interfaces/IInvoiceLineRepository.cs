using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;

public interface IInvoiceLineRepository : IBaseRepository<InvoiceLineEntity>
{
    Task<InvoiceLineEntity?> GetByIdForInvoiceAsync(Guid invoiceId, Guid lineId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InvoiceLineEntity>> GetListForInvoiceAsync(Guid invoiceId, int page, int totalRows, CancellationToken cancellationToken);
}
