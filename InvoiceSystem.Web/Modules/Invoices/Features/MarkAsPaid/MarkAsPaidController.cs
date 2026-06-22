using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Invoices.Features.MarkAsPaid;

[Route("invoices")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MarkAsPaidController(IMediator mediator) : Controller
{
    [HttpPost("pay/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new MarkAsPaidCommand(id), ct);
            TempData["SuccessMessage"] = "Faktura została pomyślnie oznaczona jako opłacona.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Index", "GetInvoiceDetails", new { id });
    }
}
