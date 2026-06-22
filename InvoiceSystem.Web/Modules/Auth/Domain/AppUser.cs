using Microsoft.AspNetCore.Identity;

namespace InvoiceSystem.Web.Modules.Auth.Domain;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
