using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;
using Invoyz.InvoiceService.Application.CQRS.Customers.Queries.Models;
using Invoyz.InvoiceService.Contracts.InboundContracts.Customers;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using Invoyz.InvoiceService.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoyz.InvoiceService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomersController(IMediator mediator) : BaseController(mediator)
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<CustomerContract>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            [FromRoute] int page,
            [FromRoute] int pageSize,
            CancellationToken cancellationToken)
            => await base.GetAsync<GetCustomersQuery,CustomerContract>(new GetCustomersQuery(page, pageSize), cancellationToken);

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerContract))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
            => await base.GetByIdAsync<GetCustomerByIdQuery, CustomerContract>(new GetCustomerByIdQuery(id), cancellationToken);

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateCustomerContract updateCustomer,
            CancellationToken cancellationToken)
            => await base.PutAsync(updateCustomer.MapToUpdateCustomerCommand(id), cancellationToken);

        [HttpPost]
        [ProducesResponseType(typeof(Guid),StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateCustomerContract createCustomer, CancellationToken cancellationToken)
            => await base.PostAsync(createCustomer.MapToCreateCustomerCommand(), cancellationToken);


        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
            => await base.DeleteAsync(new DeleteCustomerCommand(id), cancellationToken);
    }
}
