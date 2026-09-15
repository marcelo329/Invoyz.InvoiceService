using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;

namespace Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Validators;

public class DeleteInvoiceLineCommandValidator : AbstractValidator<DeleteInvoiceLineCommand>
{
    public DeleteInvoiceLineCommandValidator()
    {
        RuleFor(line => line.Id)
            .NotEmpty()
            .WithMessage("InvoiceLine Id is required.");

        RuleFor(line => line.InvoiceId)
            .NotEmpty()
            .WithMessage("InvoiceId is required");
    }
}
