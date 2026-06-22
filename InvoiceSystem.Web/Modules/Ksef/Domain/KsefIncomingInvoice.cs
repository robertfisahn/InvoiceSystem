namespace InvoiceSystem.Web.Modules.Ksef.Domain;

public enum KsefImportStatus
{
    Pending = 0,
    Imported = 1,
    Ignored = 2
}

public class KsefIncomingInvoice
{
    public int Id { get; set; }
    public string KsefNumber { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string SellerNip { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string RawXml { get; set; } = string.Empty;
    public KsefImportStatus ImportStatus { get; set; } = KsefImportStatus.Pending;
    public int? ImportedInvoiceId { get; set; }
}
