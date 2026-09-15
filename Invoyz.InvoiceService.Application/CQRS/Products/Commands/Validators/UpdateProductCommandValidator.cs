using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Validators;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(product => product.Id)
            .NotEmpty()
            .WithMessage("Product Id is required.");

        RuleFor(product => product.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(product => product.Description)
            .NotEmpty()
            .WithMessage("Description is required");

        RuleFor(product => product.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UnitPrice cannot be negative");

        RuleFor(product => product.TaxRate)
            .InclusiveBetween(0, 1)
            .WithMessage("TaxRate must be between 0 and 1");
    }
}
