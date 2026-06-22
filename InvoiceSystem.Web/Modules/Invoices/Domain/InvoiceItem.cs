namespace InvoiceSystem.Web.Modules.Invoices.Domain;

public class InvoiceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
}
