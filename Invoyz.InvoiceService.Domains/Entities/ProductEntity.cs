namespace Invoyz.InvoiceService.Domains.Entities;

public sealed class ProductEntity : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }

    public ICollection<InvoiceLineEntity> InvoiceLines { get; set; }
}
