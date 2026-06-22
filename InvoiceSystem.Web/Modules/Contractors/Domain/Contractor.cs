using InvoiceSystem.Web.Modules.Invoices.Domain;

namespace InvoiceSystem.Web.Modules.Contractors.Domain;

public class Contractor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Address { get; set; }
    
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<SoapVerificationLog> SoapVerificationLogs { get; set; } = new List<SoapVerificationLog>();
}
