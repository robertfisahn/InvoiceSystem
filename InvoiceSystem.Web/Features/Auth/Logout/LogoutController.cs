using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Web.Features.Auth.Logout;

public class LogoutController(SignInManager<AppUser> signInManager) : Controller
{
    [HttpPost("/auth/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Login");
    }
}
