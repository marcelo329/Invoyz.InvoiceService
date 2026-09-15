using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;

public record CreateInvoiceLineInput(
    Guid ProductId,
    int Quantity,
    decimal? UnitPrice,
    decimal? TaxRate);

public record CreateInvoiceCommand(
    string InvoiceNumber,
    Guid CustomerId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Status,
    IReadOnlyCollection<CreateInvoiceLineInput> Lines
) : IRequest<ErrorOr<Guid>>;
