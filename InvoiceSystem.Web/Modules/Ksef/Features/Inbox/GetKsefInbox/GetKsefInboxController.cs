using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInbox;
using InvoiceSystem.Web.Modules.Ksef.Features.Inbox.SyncKsefInbox;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInbox;

[Route("ksef/inbox")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class GetKsefInboxController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetKsefInboxQuery(), cancellationToken);
        ViewBag.KsefEnabled = result.KsefEnabled;
        ViewBag.KsefConfigured = result.KsefConfigured;

        return View(result.Invoices);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SyncKsefInboxCommand(), cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Synchronizacja zakończona pomyślnie. Pobrano {result.NewInvoicesCount} nowych faktur kosztowych.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Błąd synchronizacji: {result.ErrorMessage}";
        }

        return RedirectToAction("Index");
    }
}
