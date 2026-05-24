namespace InvoiceSystem.Web.Domain.Entities;

public class KsefSetting
{
    public int Id { get; set; }
    public string Nip { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? ActiveSessionToken { get; set; }
    public DateTime? SessionExpiresAt { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public bool IsEnabled { get; set; }
}
