using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Products.Commands.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(p => p.Description)
            .NotEmpty()
            .WithMessage("Description is required");

        RuleFor(p => p.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UnitPrice cannot be negative");

        RuleFor(p => p.TaxRate)
            .InclusiveBetween(0, 1)
            .WithMessage("TaxRate must be between 0 and 1");
    }
}
