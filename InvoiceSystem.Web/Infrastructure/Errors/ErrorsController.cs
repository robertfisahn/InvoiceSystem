using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Infrastructure.Errors;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorsController : Controller
{
    [Route("error")]
    public IActionResult Error()
    {
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var viewModel = new ErrorViewModel
        {
            TraceId = traceId,
            Message = exception?.Message ?? "Wystąpił nieoczekiwany błąd serwera.",
            Path = exceptionHandlerPathFeature?.Path
        };

        // Zwracamy widok z kodem statusu 500
        Response.StatusCode = 500;
        return View("~/Infrastructure/Errors/Error.cshtml", viewModel);
    }
}

public sealed class ErrorViewModel
{
    public required string TraceId { get; set; }
    public required string Message { get; set; }
    public string? Path { get; set; }
}
