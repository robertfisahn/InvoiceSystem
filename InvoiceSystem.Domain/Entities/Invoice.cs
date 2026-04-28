namespace InvoiceSystem.Domain.Entities;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? FilePath { get; set; }
    
    public int ContractorId { get; set; }
    public Contractor Contractor { get; set; } = null!;
    
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
