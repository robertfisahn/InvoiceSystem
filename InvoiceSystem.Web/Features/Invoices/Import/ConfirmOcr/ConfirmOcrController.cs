using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.Import.ConfirmOcr;

[Route("invoices/import/confirm")]
[Tags("Invoices")]
public sealed class ConfirmOcrController(IMediator mediator) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ConfirmOcrViewModel model, CancellationToken cancellationToken)
    {
        var command = new ConfirmOcrInvoiceCommand(
            model.SessionId,
            model.BuyerName,
            model.BuyerTaxId,
            model.BuyerAddress,
            model.Date,
            model.Items.Select(i => new ConfirmOcrItemCommand(i.Name, i.Quantity, i.UnitPrice)).ToList()
        );

        var result = await mediator.Send(command, cancellationToken);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction("Index", "ImportInvoice");
        }

        if (result.ContractorExists)
        {
            TempData["AiParsedCommand"] = result.CreateInvoiceCommandJson;
            TempData["SuccessMessage"] = "Dane faktury z analizy AI zostały zaimportowane do formularza.";
            return RedirectToAction("Index", "CreateInvoice");
        }
        else
        {
            TempData["WarningMessage"] = $"Brak kontrahenta z NIP: {model.BuyerTaxId} w bazie danych. Musisz zarejestrować go przed utworzeniem faktury.";
            return RedirectToAction("Index", "CreateContractor", new { sessionId = model.SessionId });
        }
    }
}
