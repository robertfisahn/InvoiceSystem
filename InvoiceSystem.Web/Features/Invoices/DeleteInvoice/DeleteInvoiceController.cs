using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.DeleteInvoice;

public class DeleteInvoiceController(IMediator mediator) : Controller
{
    [HttpPost("invoices/delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int id)
    {
        var result = await mediator.Send(new DeleteInvoiceCommand(id));
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Faktura została usunięta.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Error ?? "Wystąpił błąd podczas usuwania faktury.";
        }

        return RedirectToAction("Index", "GetInvoiceList");
    }
}
