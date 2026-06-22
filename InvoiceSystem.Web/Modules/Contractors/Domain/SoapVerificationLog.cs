using System;

namespace InvoiceSystem.Web.Modules.Contractors.Domain;

public class SoapVerificationLog
{
    public Guid Id { get; set; }
    public int ContractorId { get; set; }
    public string NipQueried { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty; // "WCF" lub "RawHttp"
    public string RequestEnvelope { get; set; } = string.Empty; // Surowy XML żądania
    public string ResponseEnvelope { get; set; } = string.Empty; // Surowy XML odpowiedzi
    public bool IsValid { get; set; }
    public string? VerificationCode { get; set; }
    public string? ErrorMessage { get; set; } // SOAP FaultString w razie błędu
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Contractor Contractor { get; set; } = null!;
}
