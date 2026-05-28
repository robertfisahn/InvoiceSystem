using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceDetails;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceDetails;

[Route("ksef/inbox")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class GetKsefInvoiceDetailsController(IMediator mediator) : Controller
{
    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetKsefInvoiceDetailsQuery(id), cancellationToken);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = $"Błąd podczas ładowania szczegółów: {result.ErrorMessage}";
            return RedirectToAction("Index", "GetKsefInbox");
        }

        ViewBag.Id = result.Id;
        ViewBag.KsefNumber = result.KsefNumber;
        ViewBag.ImportStatus = result.ImportStatus;
        ViewBag.IssueDate = result.IssueDate;

        return View(result.ParsedInvoice);
    }
}
