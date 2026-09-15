using ErrorOr;
using MediatR;

namespace Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;

public record UpdateInvoiceCommand(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    string Status) : BaseCQRSWithId(Id), IRequest<Error?>;
