using Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;
using Invoyz.InvoiceService.Application.CQRS.Products.Queries.Models;
using Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Products;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using Invoyz.InvoiceService.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoyz.InvoiceService.Controllers
{
    public class ProductsController(IMediator mediator) : BaseController(mediator)
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<ProductContract>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            CancellationToken cancellationToken,
            [FromQuery] ushort page = 1,
            [FromQuery] ushort pageSize = 10)
            => await base.GetAsync<GetProductsQuery, ProductContract>(new GetProductsQuery(page, pageSize), cancellationToken);

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductContract))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
            => await base.GetByIdAsync<GetProductByIdQuery, ProductContract>(new GetProductByIdQuery(id), cancellationToken);

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(
            [FromBody] CreateProductContract createProduct,
            CancellationToken cancellationToken)
            => await base.PostAsync(createProduct.MapToCreateProductCommand(), cancellationToken);

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] UpdateProductContract updateProduct,
            CancellationToken cancellationToken)
            => await base.PutAsync(updateProduct.MapToUpdateProductCommand(id), cancellationToken);

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
            => await base.DeleteAsync(new DeleteProductCommand(id), cancellationToken);
    }
}
