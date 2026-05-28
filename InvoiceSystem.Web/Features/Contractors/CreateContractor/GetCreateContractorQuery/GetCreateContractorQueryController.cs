using System;
using InvoiceSystem.Web.Features.Invoices.Import;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace InvoiceSystem.Web.Features.Contractors.CreateContractor.GetCreateContractorQuery;

[Route("contractors/create")]
[Tags("Contractors")]
public sealed class GetCreateContractorQueryController(IMemoryCache cache) : Controller
{
    [HttpGet]
    public IActionResult Index([FromQuery] string? sessionId)
    {
        var command = new CreateContractorCommand.CreateContractorCommand();

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var cacheKey = $"ocr-session-{sessionId}";
            if (cache.TryGetValue<OcrSessionData>(cacheKey, out var sessionData) && sessionData is not null)
            {
                command = new CreateContractorCommand.CreateContractorCommand
                {
                    SessionId = sessionId,
                    Name = sessionData.BuyerName,
                    TaxId = sessionData.BuyerTaxId,
                    Address = sessionData.BuyerAddress
                };
                
                ViewBag.WarningMessage = $"Brak kontrahenta z NIP: {sessionData.BuyerTaxId} w bazie danych. Dane zostały wyodrębnione przez AI. Zweryfikuj i zapisz kontrahenta.";
            }
            else
            {
                TempData["ErrorMessage"] = "Sesja analizy OCR wygasła lub jest nieprawidłowa.";
                return RedirectToAction("Index", "ImportInvoice");
            }
        }

        return View(command); // Resolves automatically to Index.cshtml in this directory!
    }
}
