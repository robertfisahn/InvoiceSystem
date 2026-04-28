namespace InvoiceSystem.Domain.Entities;

public class Contractor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Address { get; set; }
    
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
