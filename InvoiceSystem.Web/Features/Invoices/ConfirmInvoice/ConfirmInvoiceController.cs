using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.ConfirmInvoice;

[Route("invoices")]
public sealed class ConfirmInvoiceController(IMediator mediator) : Controller
{
    [HttpPost("confirm/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new ConfirmInvoiceCommand(id), ct);
            TempData["SuccessMessage"] = "Faktura została pomyślnie zatwierdzona. Edycja została zablokowana.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction("Index", "GetInvoiceDetails", new { id });
    }
}
