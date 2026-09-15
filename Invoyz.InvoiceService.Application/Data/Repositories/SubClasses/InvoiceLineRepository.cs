using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Data.Repositories.SubClasses;

public sealed class InvoiceLineRepository : BaseRepository<InvoiceLineEntity>, IInvoiceLineRepository
{
    public InvoiceLineRepository(AppDbContext appDbContext) : base(appDbContext) { }
}

