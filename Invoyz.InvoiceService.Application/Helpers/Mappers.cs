using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using Invoyz.InvoiceService.Domains.Entities;

namespace Invoyz.InvoiceService.Application.Helpers;

public static class Mappers
{
    public static CustomerContract ToCustomerContract(this CustomerEntity customer)
        => new CustomerContract(
                Address: customer.Address,
                CreatedAt: customer.CreatedAt,
                Email: customer.Email,
                LastModifiedAt: customer.LastModifiedAt,
                Name: customer.Name,
                VatNumber: customer.VatNumber);

    public static ProductContract ToProductContract(this ProductEntity product)
        => new ProductContract(
                Id: product.Id,
                Name: product.Name,
                Description: product.Description,
                UnitPrice: product.UnitPrice,
                TaxRate: product.TaxRate,
                CreatedAt: product.CreatedAt,
                LastModifiedAt: product.LastModifiedAt);

    public static InvoiceLineContract ToInvoiceLineContract(this InvoiceLineEntity line)
        => new InvoiceLineContract(
                Id: line.Id,
                InvoiceId: line.InvoiceId,
                ProductId: line.ProductId,
                Quantity: line.Quantity,
                UnitPrice: line.UnitPrice,
                TaxRate: line.TaxRate,
                LineTotal: line.LineTotal,
                LineTax: line.LineTax,
                CreatedAt: line.CreatedAt,
                LastModifiedAt: line.LastModifiedAt);

    public static InvoiceContract ToInvoiceContract(this InvoiceEntity invoice)
        => new InvoiceContract(
                Id: invoice.Id,
                InvoiceNumber: invoice.InvoiceNumber,
                CustomerId: invoice.CustomerId,
                IssueDate: invoice.IssueDate,
                DueDate: invoice.DueDate,
                Status: invoice.Status,
                SubTotal: invoice.SubTotal,
                TaxTotal: invoice.TaxTotal,
                GrandTotal: invoice.GrandTotal,
                Lines: invoice.InvoiceLines is null
                    ? []
                    : [.. invoice.InvoiceLines
                        .Where(line => !line.IsDeleted)
                        .Select(line => line.ToInvoiceLineContract())],
                CreatedAt: invoice.CreatedAt,
                LastModifiedAt: invoice.LastModifiedAt);
}
