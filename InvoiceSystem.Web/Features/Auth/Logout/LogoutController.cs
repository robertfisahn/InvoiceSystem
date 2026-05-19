using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Web.Domain.Entities;

namespace InvoiceSystem.Web.Features.Auth.Logout;

[Route("/auth/logout")]
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
