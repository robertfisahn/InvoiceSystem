namespace InvoiceSystem.Domain.Entities;

public class InvoiceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
}
