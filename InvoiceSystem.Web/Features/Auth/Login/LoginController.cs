using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Auth.Login;

[AllowAnonymous]
[Route("/auth/login")]
public sealed class LoginController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/dashboard");

        return View(new LoginCommand("", ""));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([FromForm] LoginCommand command)
    {
        try
        {
            var result = await mediator.Send(command);

            if (result.Success)
                return Redirect("/dashboard");

            ModelState.AddModelError(string.Empty, result.Error ?? "Błąd logowania.");
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }
        
        return View(command);
    }
}
