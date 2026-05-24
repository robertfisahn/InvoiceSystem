using System;
using System.ComponentModel.DataAnnotations;

namespace InvoiceSystem.Web.Features.Ksef.Configuration;

public sealed class KsefConfigurationViewModel
{
    [Required(ErrorMessage = "Pole NIP jest wymagane.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "NIP musi składać się z dokładnie 10 cyfr.")]
    public string Nip { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pole Token Autoryzacyjny KSeF jest wymagane.")]
    public string ApiKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    // Read-only connection status
    public string? ActiveSessionToken { get; set; }
    public DateTime? SessionExpiresAt { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public bool IsConnected => !string.IsNullOrEmpty(ActiveSessionToken) && SessionExpiresAt > DateTime.UtcNow;
}
