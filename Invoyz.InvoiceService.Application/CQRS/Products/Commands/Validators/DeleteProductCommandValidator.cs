using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Validators;

public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
        => RuleFor(product => product.Id)
            .NotEmpty()
            .WithMessage("Product Id is required.");
}
