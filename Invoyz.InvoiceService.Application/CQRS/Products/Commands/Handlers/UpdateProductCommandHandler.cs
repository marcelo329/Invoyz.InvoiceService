using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Handlers;

public sealed class UpdateProductCommandHandler(
    IProductRepository _productRepository,
    ILogger<UpdateProductCommandHandler> _logger) : IRequestHandler<UpdateProductCommand, Error?>
{
    public async Task<Error?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var msg = "Failed to update product";

        _logger.LogInformation("Starting Product update process.");

        try
        {
            var product = await _productRepository.GetById(request.Id, cancellationToken);

            if (product == null)
                return Error.NotFound($"Product with id {request.Id} not found.");

            var sameName = request.Name.Equals(product.Name);
            var sameDescription = request.Description.Equals(product.Description);
            var sameUnitPrice = request.UnitPrice == product.UnitPrice;
            var sameTaxRate = request.TaxRate == product.TaxRate;

            if (sameName && sameDescription && sameUnitPrice && sameTaxRate)
                return Error.Conflict("No changes detected. No changes applied.");

            product.Name = request.Name;
            product.Description = request.Description;
            product.UnitPrice = request.UnitPrice;
            product.TaxRate = request.TaxRate;

            var errorUpdating = await _productRepository.UpdateAsync(product, cancellationToken);

            if (errorUpdating != null)
            {
                _logger.LogError("Error updating.Error:" + errorUpdating.Value.Code);
                return errorUpdating;
            }

            _logger.LogInformation("Product with Id {productId} updated successfully", request.Id);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, msg);
            return Error.Failure($"{msg}.Error:" + ex.Message);
        }
    }
}
