using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;

public sealed class InvoiceRepository : BaseRepository<InvoiceEntity>, IInvoiceRepository
{
    public InvoiceRepository(AppDbContext appDbContext) : base(appDbContext) { }
}

