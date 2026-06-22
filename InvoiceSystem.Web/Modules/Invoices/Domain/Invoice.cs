using System;
using System.Collections.Generic;
using InvoiceSystem.Web.Modules.Contractors.Domain;

namespace InvoiceSystem.Web.Modules.Invoices.Domain;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? FilePath { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    
    public int ContractorId { get; set; }
    public Contractor Contractor { get; set; } = null!;
    
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    // KSeF Metadata
    public string? KsefNumber { get; set; }
    public string? KsefTransactionId { get; set; }
    public DateTime? KsefSentAt { get; set; }
    public string? UpoXml { get; set; }
}
