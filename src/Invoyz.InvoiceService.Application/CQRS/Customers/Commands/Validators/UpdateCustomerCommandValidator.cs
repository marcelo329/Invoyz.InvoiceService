using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Validators;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(customer => customer.Id)
            .NotEmpty()
            .WithMessage("Customer Id is required.");

        RuleFor(customer => customer)
            .SetValidator(new CreateCustomerCommandValidator());
    }
}
