using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Validators;

public class UpdateInvoiceLineCommandValidator : AbstractValidator<UpdateInvoiceLineCommand>
{
    public UpdateInvoiceLineCommandValidator()
    {
        RuleFor(line => line.Id)
            .NotEmpty()
            .WithMessage("InvoiceLine Id is required.");

        RuleFor(line => line.InvoiceId)
            .NotEmpty()
            .WithMessage("InvoiceId is required");

        RuleFor(line => line.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required");

        RuleFor(line => line.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");

        RuleFor(line => line.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .When(line => line.UnitPrice.HasValue)
            .WithMessage("UnitPrice cannot be negative");

        RuleFor(line => line.TaxRate)
            .InclusiveBetween(0, 1)
            .When(line => line.TaxRate.HasValue)
            .WithMessage("TaxRate must be between 0 and 1");
    }
}
