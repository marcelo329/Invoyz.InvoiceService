using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Validators;

public class UpdateInvoiceCommandValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceCommandValidator()
    {
        RuleFor(invoice => invoice.Id)
            .NotEmpty()
            .WithMessage("Invoice Id is required.");

        RuleFor(invoice => invoice.InvoiceNumber)
            .NotEmpty()
            .WithMessage("InvoiceNumber is required");

        RuleFor(invoice => invoice.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required");

        RuleFor(invoice => invoice.DueDate)
            .GreaterThanOrEqualTo(invoice => invoice.IssueDate)
            .WithMessage("DueDate cannot be earlier than IssueDate");

        RuleFor(invoice => invoice.Status)
            .Must(InvoiceStatuses.IsAllowed)
            .WithMessage($"Status must be one of: {InvoiceStatuses.Allowed}");
    }
}
