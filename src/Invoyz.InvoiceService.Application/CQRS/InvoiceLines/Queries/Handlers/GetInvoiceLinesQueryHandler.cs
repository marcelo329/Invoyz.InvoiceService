using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using MediatR;
using System.Collections.Immutable;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Queries.Handlers;

public sealed class GetInvoiceLinesQueryHandler(IInvoiceLineRepository invoiceLineRepository) : IRequestHandler<GetInvoiceLinesQuery, IReadOnlyCollection<InvoiceLineContract>>
{
    public async Task<IReadOnlyCollection<InvoiceLineContract>> Handle(GetInvoiceLinesQuery request, CancellationToken cancellationToken)
    {
        var result = await invoiceLineRepository.GetListForInvoiceAsync(request.InvoiceId, request.Page, request.PageSize, cancellationToken);

        return result
            .Select(a => a.ToInvoiceLineContract())
            .ToImmutableArray();
    }
}
