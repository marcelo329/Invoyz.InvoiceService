using Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;
using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;

public interface IInvoiceRepository : IBaseRepository<InvoiceEntity>
{
    Task<InvoiceEntity?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken);

    Task<InvoiceEntity?> GetByIdWithLinesAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<InvoiceEntity>> GetListWithLinesAsync(int page, int totalRows, CancellationToken cancellationToken);

    Task<GetInvoiceEagerLoadingDTO?> GetEagerLoadingAsync(Guid Id, CancellationToken cancellationToken);
}
