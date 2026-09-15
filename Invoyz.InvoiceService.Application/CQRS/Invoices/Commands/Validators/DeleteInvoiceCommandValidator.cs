using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Validators;

public class DeleteInvoiceCommandValidator : AbstractValidator<DeleteInvoiceCommand>
{
    public DeleteInvoiceCommandValidator()
        => RuleFor(invoice => invoice.Id)
            .NotEmpty()
            .WithMessage("Invoice Id is required.");
}
