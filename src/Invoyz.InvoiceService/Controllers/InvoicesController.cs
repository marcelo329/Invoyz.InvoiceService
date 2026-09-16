using Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Queries.Models;
using Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Invoices;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using Invoyz.InvoiceService.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoyz.InvoiceService.Controllers
{
    public class InvoicesController(IMediator mediator) : BaseController(mediator)
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<InvoiceContract>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            CancellationToken cancellationToken,
            [FromQuery] ushort page = 1,
            [FromQuery] ushort pageSize = 10)
            => await base.GetAsync<GetInvoicesQuery, InvoiceContract>(new GetInvoicesQuery(page, pageSize), cancellationToken);

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InvoiceContract))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
            => await base.GetByIdAsync<GetInvoiceByIdQuery, InvoiceContract>(new GetInvoiceByIdQuery(id), cancellationToken);

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] CreateInvoiceContract createInvoice,
            CancellationToken cancellationToken)
            => await base.PostAsync(createInvoice.MapToCreateInvoiceCommand(), cancellationToken);

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateInvoiceContract updateInvoice,
            CancellationToken cancellationToken)
            => await base.PutAsync(updateInvoice.MapToUpdateInvoiceCommand(id), cancellationToken);

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
            => await base.DeleteAsync(new DeleteInvoiceCommand(id), cancellationToken);
    }
}
