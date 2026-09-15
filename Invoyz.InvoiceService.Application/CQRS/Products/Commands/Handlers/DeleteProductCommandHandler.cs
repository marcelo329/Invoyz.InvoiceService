using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Handlers;

public sealed class DeleteProductCommandHandler(
    IProductRepository _productRepository,
    ILogger<DeleteProductCommandHandler> _logger) : IRequestHandler<DeleteProductCommand, Error?>
{
    public async Task<Error?> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start product {productId} deletion process.", request.Id);

        try
        {
            var product = await _productRepository.GetById(request.Id, cancellationToken);

            if (product == null)
                return Error.NotFound($"Product with id {request.Id} not found.");

            // A product still referenced by a live invoice line cannot be removed:
            // the line's price history would lose the product it points at.
            if (await _productRepository.IsReferencedByInvoiceLineAsync(request.Id, cancellationToken))
                return Error.Conflict($"Product with id {request.Id} is referenced by invoice lines and cannot be deleted.");

            var deleted = await _productRepository.DeleteAsync(product, cancellationToken);

            if (!deleted)
            {
                _logger.LogWarning("Failed to delete product {productId}.", request.Id);
                return Error.Failure($"Failed to delete product with id {request.Id}.");
            }

            _logger.LogInformation("Product {productId} deleted successfully.", request.Id);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed deletion process for product {productId}.", request.Id);
            return Error.Failure($"Failed to delete product with id {request.Id}.Error:" + ex.Message);
        }
    }
}
