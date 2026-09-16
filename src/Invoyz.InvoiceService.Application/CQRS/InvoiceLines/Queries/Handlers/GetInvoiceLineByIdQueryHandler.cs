using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Queries.Handlers;

public sealed class GetInvoiceLineByIdQueryHandler(IInvoiceLineRepository invoiceLineRepository) : IRequestHandler<GetInvoiceLineByIdQuery, ErrorOr<InvoiceLineContract>>
{
    public async Task<ErrorOr<InvoiceLineContract>> Handle(GetInvoiceLineByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await invoiceLineRepository.GetByIdForInvoiceAsync(request.InvoiceId, request.Id, cancellationToken);

        if (result == null)
            return Error.NotFound($"Invoice line with id {request.Id} not found on invoice {request.InvoiceId}.");

        return result.ToInvoiceLineContract();
    }
}
