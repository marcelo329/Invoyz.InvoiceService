namespace Invoyz.InvoiceService.Domains.Entities;

public sealed class InvoiceEntity : BaseEntity
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; }

    public Guid CustomerId { get; set; }
    public CustomerEntity Customer { get; set; }

    public DateTimeOffset DueDate { get; set; }
    public DateTimeOffset IssueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Status { get; set; }

    public ICollection<InvoiceLineEntity> InvoiceLines { get; set; }
}
