namespace Invoyz.InvoiceService.Domains.Entities;

public sealed class InvoiceLineEntity : BaseEntity
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }
    public InvoiceEntity Invoice { get; set; }

    public Guid ProductId { get; set; }
    public ProductEntity Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
    public decimal LineTax { get; set; }
}
