using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef.SendInvoiceToKsef;

[Route("invoices")]
[Tags("Invoices - KSeF")]
public sealed class SendInvoiceToKsefController(IMediator mediator) : Controller
{
    [HttpPost("{id:int}/send-to-ksef")]
    public async Task<IActionResult> Index(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SendInvoiceToKsefCommand(id), cancellationToken);
        if (result.Success)
        {
            if (!string.IsNullOrEmpty(result.KsefNumber))
            {
                TempData["SuccessMessage"] = $"Faktura wysłana do KSeF. Nadano numer KSeF: {result.KsefNumber} i wygenerowano UPO.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Faktura przekazana do KSeF. ID transakcji: {result.TransactionId}. Oczekuje na przetworzenie.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Błąd podczas wysyłki do KSeF.";
        }

        return RedirectToAction("Index", "GetInvoiceDetails", new { id });
    }
}
