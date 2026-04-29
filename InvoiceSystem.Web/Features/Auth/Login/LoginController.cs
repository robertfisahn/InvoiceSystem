using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Auth.Login;

[AllowAnonymous]
public class LoginController(IMediator mediator) : Controller
{
    [HttpGet("/auth/login")]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "GetInvoiceList");

        return View(new LoginCommand("", ""));
    }

    [HttpPost("/auth/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([FromForm] LoginCommand command)
    {
        try
        {
            var result = await mediator.Send(command);

            if (result.Success)
                return RedirectToAction("Index", "GetInvoiceList");

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
