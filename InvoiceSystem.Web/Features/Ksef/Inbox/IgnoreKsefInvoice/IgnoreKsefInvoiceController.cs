using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Features.Ksef.Inbox.IgnoreKsefInvoice;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.IgnoreKsefInvoice;

[Route("ksef/inbox")]
public sealed class IgnoreKsefInvoiceController(IMediator mediator) : Controller
{
    [HttpPost("ignore/{id:int}")]
    public async Task<IActionResult> Ignore(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new IgnoreKsefInvoiceCommand(id), cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Faktura została oznaczona jako zignorowana.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }

        return RedirectToAction("Index", "GetKsefInbox");
    }
}
