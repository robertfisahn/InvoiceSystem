using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Web.Modules.Auth.Domain;

namespace InvoiceSystem.Web.Modules.Auth.Features.Logout;

[Route("/auth/logout")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class LogoutController(SignInManager<AppUser> signInManager) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Login");
    }
}
