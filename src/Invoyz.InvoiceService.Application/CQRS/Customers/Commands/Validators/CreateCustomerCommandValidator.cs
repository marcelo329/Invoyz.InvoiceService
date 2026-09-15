using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Validators;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(p => p.Address)
            .NotEmpty()
            .WithMessage("Address is required");

        RuleFor(p => p.Email)
            .NotEmpty()
            .WithMessage("Email is required");

        RuleFor(p => p.Email)
            .EmailAddress()
            .When(p => !string.IsNullOrEmpty(p.Email));

        RuleFor(p => p.VatNumber)
            .NotEmpty()
            .WithMessage("VatNumber is required");
    }
}
