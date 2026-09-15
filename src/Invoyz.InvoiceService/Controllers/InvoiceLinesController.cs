using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;
using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Queries.Models;
using Invoyz.InvoiceService.Contracts.InboundContracts.InvoiceLines;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using Invoyz.InvoiceService.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoyz.InvoiceService.Controllers
{
    // Lines are a sub-resource of an invoice, so this is the one controller that declares
    // its own template. It repeats the version segment deliberately: a derived [Route]
    // REPLACES the BaseController template rather than combining with it, and omitting
    // the version here would silently unversion every action below.
    [Route("api/v{version:apiVersion}/Invoices/{invoiceId:guid}/Lines")]
    public class InvoiceLinesController(IMediator mediator) : BaseController(mediator)
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<InvoiceLineContract>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            [FromRoute] Guid invoiceId,
            CancellationToken cancellationToken,
            [FromQuery] ushort page = 1,
            [FromQuery] ushort pageSize = 10)
            => await base.GetAsync<GetInvoiceLinesQuery, InvoiceLineContract>(
                new GetInvoiceLinesQuery(invoiceId, page, pageSize), cancellationToken);

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InvoiceLineContract))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            [FromRoute] Guid invoiceId,
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
            => await base.GetByIdAsync<GetInvoiceLineByIdQuery, InvoiceLineContract>(
                new GetInvoiceLineByIdQuery(id, invoiceId), cancellationToken);

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromRoute] Guid invoiceId,
            [FromBody] CreateInvoiceLineContract createInvoiceLine,
            CancellationToken cancellationToken)
            => await base.PostAsync(createInvoiceLine.MapToCreateInvoiceLineCommand(invoiceId), cancellationToken);

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            [FromRoute] Guid invoiceId,
            [FromRoute] Guid id,
            [FromBody] UpdateInvoiceLineContract updateInvoiceLine,
            CancellationToken cancellationToken)
            => await base.PutAsync(updateInvoiceLine.MapToUpdateInvoiceLineCommand(invoiceId, id), cancellationToken);

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid invoiceId,
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
            => await base.DeleteAsync(new DeleteInvoiceLineCommand(id, invoiceId), cancellationToken);
    }
}
