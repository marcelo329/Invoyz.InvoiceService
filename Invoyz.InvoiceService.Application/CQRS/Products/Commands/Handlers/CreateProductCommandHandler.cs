using ErrorOr;
using Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;
using Invoyz.InvoiceService.Application.Data.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Handlers;

public sealed class CreateProductCommandHandler(
    IProductRepository _productRepository,
    ILogger<CreateProductCommandHandler> _logger) : IRequestHandler<CreateProductCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Product creation process.");
        var msg = "Failed to create product";

        try
        {
            var result = await _productRepository.CreateAsync(new Domains.Entities.ProductEntity
            {
                Name = request.Name,
                Description = request.Description,
                UnitPrice = request.UnitPrice,
                TaxRate = request.TaxRate
            }, cancellationToken);

            _logger.LogInformation("Product created successfully with Id {productId}.", result.Entity.Id);

            return result.Entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, msg);
            return Error.Failure($"{msg}.Error:" + ex.Message);
        }
    }
}
