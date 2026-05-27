using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef;

[Route("invoices")]
public sealed class SendToKsefController(IMediator mediator) : Controller
{
    [HttpPost("{id:int}/send-to-ksef")]
    public async Task<IActionResult> Send(int id, CancellationToken cancellationToken)
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

    [HttpGet("{id:int}/ksef-xml")]
    public async Task<IActionResult> DownloadXml(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DownloadInvoiceKsefXmlQuery(id), cancellationToken);
        if (!result.Success)
            return NotFound(result.ErrorMessage);

        var bytes = System.Text.Encoding.UTF8.GetBytes(result.Xml!);
        return File(bytes, "application/xml", $"KSEF-{result.InvoiceNumber}.xml");
    }

    [HttpGet("{id:int}/ksef-upo")]
    public async Task<IActionResult> DownloadUpo(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DownloadInvoiceKsefUpoQuery(id), cancellationToken);
        if (!result.Success)
            return NotFound(result.ErrorMessage);

        var bytes = System.Text.Encoding.UTF8.GetBytes(result.UpoXml!);
        return File(bytes, "application/xml", $"UPO-{result.KsefNumber}.xml");
    }
}
