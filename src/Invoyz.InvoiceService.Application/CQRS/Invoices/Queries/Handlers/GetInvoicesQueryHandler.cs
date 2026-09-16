using Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using MediatR;
using System.Collections.Immutable;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Handlers;

public sealed class GetInvoicesQueryHandler(IInvoiceRepository invoiceRepository) : IRequestHandler<GetInvoicesQuery, IReadOnlyCollection<InvoiceContract>>
{
    public async Task<IReadOnlyCollection<InvoiceContract>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var result = await invoiceRepository.GetListWithLinesAsync(request.Page, request.PageSize, cancellationToken);

        return result
            .Select(a => a.ToInvoiceContract())
            .OrderBy(a => a.InvoiceNumber)
            .ToImmutableArray();
    }
}
