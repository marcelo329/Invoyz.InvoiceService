using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Models;
using Invoyz.InvoiceService.Application.CQRS.InvoiceLines.Commands.Models;
using Invoyz.InvoiceService.Application.CQRS.Invoices.Commands.Models;
using Invoyz.InvoiceService.Application.CQRS.Products.Commands.Models;
using Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Customers;
using Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.InvoiceLines;
using Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Invoices;
using Invoyz.InvoiceService.Contracts.RestAPI.InboundContracts.Products;

namespace Invoyz.InvoiceService.Extensions;

public static class Mappers
{
    public static CreateCustomerCommand MapToCreateCustomerCommand(this CreateCustomerContract contract)
        => new CreateCustomerCommand(
            Name: contract.Name,
            Email: contract.Email,
            Address: contract.Address,
            VatNumber: contract.VatNumber
        );

    public static UpdateCustomerCommand MapToUpdateCustomerCommand(this UpdateCustomerContract contract, Guid id)
        => new UpdateCustomerCommand(
            Id: id,
            Name: contract.Name,
            Email: contract.Email,
            Address: contract.Address,
            VatNumber: contract.VatNumber
        );

    public static CreateProductCommand MapToCreateProductCommand(this CreateProductContract contract)
        => new CreateProductCommand(
            Name: contract.Name,
            Description: contract.Description,
            UnitPrice: contract.UnitPrice,
            TaxRate: contract.TaxRate
        );

    public static UpdateProductCommand MapToUpdateProductCommand(this UpdateProductContract contract, Guid id)
        => new UpdateProductCommand(
            Id: id,
            Name: contract.Name,
            Description: contract.Description,
            UnitPrice: contract.UnitPrice,
            TaxRate: contract.TaxRate
        );

    public static CreateInvoiceCommand MapToCreateInvoiceCommand(this CreateInvoiceContract contract)
        => new CreateInvoiceCommand(
            InvoiceNumber: contract.InvoiceNumber,
            CustomerId: contract.CustomerId,
            IssueDate: contract.IssueDate,
            DueDate: contract.DueDate,
            Status: contract.Status,
            Lines: contract.Lines is null
                ? []
                : [.. contract.Lines.Select(line => new CreateInvoiceLineInput(
                    ProductId: line.ProductId,
                    Quantity: line.Quantity,
                    UnitPrice: line.UnitPrice,
                    TaxRate: line.TaxRate))]
        );

    public static UpdateInvoiceCommand MapToUpdateInvoiceCommand(this UpdateInvoiceContract contract, Guid id)
        => new UpdateInvoiceCommand(
            Id: id,
            InvoiceNumber: contract.InvoiceNumber,
            CustomerId: contract.CustomerId,
            IssueDate: contract.IssueDate,
            DueDate: contract.DueDate,
            Status: contract.Status
        );

    public static CreateInvoiceLineCommand MapToCreateInvoiceLineCommand(this CreateInvoiceLineContract contract, Guid invoiceId)
        => new CreateInvoiceLineCommand(
            InvoiceId: invoiceId,
            ProductId: contract.ProductId,
            Quantity: contract.Quantity,
            UnitPrice: contract.UnitPrice,
            TaxRate: contract.TaxRate
        );

    public static UpdateInvoiceLineCommand MapToUpdateInvoiceLineCommand(this UpdateInvoiceLineContract contract, Guid invoiceId, Guid id)
        => new UpdateInvoiceLineCommand(
            Id: id,
            InvoiceId: invoiceId,
            ProductId: contract.ProductId,
            Quantity: contract.Quantity,
            UnitPrice: contract.UnitPrice,
            TaxRate: contract.TaxRate
        );
}
