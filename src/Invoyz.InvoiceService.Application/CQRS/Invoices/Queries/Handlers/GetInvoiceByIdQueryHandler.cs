using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Handlers;

public sealed class GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepository) : IRequestHandler<GetInvoiceByIdQuery, ErrorOr<InvoiceContract>>
{
    public async Task<ErrorOr<InvoiceContract>> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await invoiceRepository.GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (result == null)
            return Error.NotFound($"Invoice with id {request.Id} not found.");

        return result.ToInvoiceContract();
    }
}
