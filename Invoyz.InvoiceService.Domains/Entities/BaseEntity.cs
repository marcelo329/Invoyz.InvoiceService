namespace Invoyz.InvoiceService.Domains.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastModifiedAt { get; set; }
}
