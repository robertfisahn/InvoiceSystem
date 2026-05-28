using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.DeleteInvoice;

[Route("invoices/delete")]
[Microsoft.AspNetCore.Http.Tags("Invoices")]
public sealed class DeleteInvoiceController(IMediator mediator) : Controller
{
    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int id)
    {
        await mediator.Send(new DeleteInvoiceCommand(id));
        TempData["SuccessMessage"] = "Faktura została usunięta.";
        return RedirectToAction("Index", "GetInvoiceList");
    }
}
