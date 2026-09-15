using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Validators;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(invoice => invoice.InvoiceNumber)
            .NotEmpty()
            .WithMessage("InvoiceNumber is required");

        RuleFor(invoice => invoice.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required");

        RuleFor(invoice => invoice.IssueDate)
            .NotEmpty()
            .WithMessage("IssueDate is required");

        RuleFor(invoice => invoice.DueDate)
            .GreaterThanOrEqualTo(invoice => invoice.IssueDate)
            .WithMessage("DueDate cannot be earlier than IssueDate");

        RuleFor(invoice => invoice.Status)
            .Must(InvoiceStatuses.IsAllowed)
            .WithMessage($"Status must be one of: {InvoiceStatuses.Allowed}");

        RuleFor(invoice => invoice.Lines)
            .NotEmpty()
            .WithMessage("An invoice requires at least one line");

        RuleForEach(invoice => invoice.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .NotEmpty()
                .WithMessage("ProductId is required");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero");

            line.RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .When(l => l.UnitPrice.HasValue)
                .WithMessage("UnitPrice cannot be negative");

            line.RuleFor(l => l.TaxRate)
                .InclusiveBetween(0, 1)
                .When(l => l.TaxRate.HasValue)
                .WithMessage("TaxRate must be between 0 and 1");
        });
    }
}
