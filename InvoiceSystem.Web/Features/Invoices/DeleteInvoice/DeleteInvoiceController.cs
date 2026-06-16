using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.DeleteInvoice;

[Route("invoices/delete")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class DeleteInvoiceController(IMediator mediator) : Controller
{
    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int id)
    {
        try
        {
            await mediator.Send(new DeleteInvoiceCommand(id));
            TempData["SuccessMessage"] = "Faktura została usunięta.";
        }
        catch (System.InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction("Index", "GetInvoiceList");
    }
}
