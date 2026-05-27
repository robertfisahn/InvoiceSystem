using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Features.Ksef.Inbox.ImportKsefInvoice;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.ImportKsefInvoice;

[Route("ksef/inbox")]
public sealed class ImportKsefInvoiceController(IMediator mediator) : Controller
{
    [HttpPost("import/{id:int}")]
    public async Task<IActionResult> Import(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ImportKsefInvoiceCommand(id), cancellationToken);
        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Faktura {result.InvoiceNumber} od {result.SellerName} została zaimportowana do systemu.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Wystąpił błąd podczas importowania: {result.ErrorMessage}";
        }

        return RedirectToAction("Index", "GetKsefInbox");
    }
}
