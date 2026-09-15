namespace Invoyz.InvoiceService.Domains.Entities;

public sealed class CustomerEntity : BaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string VatNumber { get; set; }
    public string DeveloperName { get; set; } = "Marcelo";
}
