using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Ksef.Features.Configuration.GetKsefConfiguration;
using InvoiceSystem.Web.Modules.Ksef.Features.Configuration.SaveKsefConfiguration;
using InvoiceSystem.Web.Modules.Ksef.Features.Configuration.TestKsefConnection;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Configuration;

[Route("ksef")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class KsefConfigurationController(IMediator mediator) : Controller
{
    [HttpGet("config")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewModel = await mediator.Send(new GetKsefConfigurationQuery(), cancellationToken);
        return View(viewModel);
    }

    [HttpPost("config")]
    public async Task<IActionResult> Save(KsefConfigurationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        await mediator.Send(new SaveKsefConfigurationCommand(model), cancellationToken);
        TempData["SuccessMessage"] = "Ustawienia KSeF zostały zapisane.";

        return RedirectToAction("Index");
    }
}
