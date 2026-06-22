using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Invoices.Features.Import.ImportInvoice;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace InvoiceSystem.Web.Modules.Invoices.Features.Import.AnalyzeOcr;

[Route("invoices/import/analyze")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class AnalyzeOcrController(
    IMediator mediator,
    IMemoryCache cache
) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string extractedText, string provider, string filePath, string documentType, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                TempData["ErrorMessage"] = "Brak tekstu do analizy.";
                return RedirectToAction("Index", "ImportInvoice");
            }

            var result = await mediator.Send(new AnalyzeOcrTextCommand(extractedText, provider), cancellationToken);

            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = $"Analiza AI ({provider}) nie powiodła się: {result.ErrorMessage ?? "Nie udało się sparsować dokumentu."}";
                TempData["ExtractedText"] = extractedText;
                TempData["DocumentType"] = documentType;
                TempData["FilePath"] = filePath;
                return RedirectToAction("Index", "ImportInvoice");
            }

            var dto = result.Data;
            var sessionId = Guid.NewGuid().ToString();

            var sessionData = new OcrSessionData
            {
                FilePath = filePath,
                BuyerName = dto.BuyerName ?? string.Empty,
                BuyerTaxId = CleanTaxId(dto.BuyerTaxId),
                BuyerAddress = dto.BuyerAddress ?? string.Empty,
                InvoiceNumber = dto.InvoiceNumber ?? string.Empty,
                Date = dto.Date ?? DateTime.Today,
                Items = dto.Items.Select(i => new OcrSessionItem
                {
                    Name = i.Name ?? "Pozycja z OCR",
                    Quantity = i.Quantity <= 0 ? 1 : i.Quantity,
                    UnitPrice = i.UnitPrice < 0 ? 0 : i.UnitPrice
                }).ToList()
            };

            // Zapisujemy do pamięci podręcznej na 30 minut
            cache.Set($"ocr-session-{sessionId}", sessionData, TimeSpan.FromMinutes(30));

            var viewModel = new ImportInvoiceViewModel
            {
                SuccessMessage = $"Analiza AI ({provider}) zakończona pomyślnie. Zweryfikuj dane poniżej.",
                ExtractedText = extractedText,
                DocumentType = documentType,
                FilePath = filePath,
                SessionId = sessionId,
                ExtractedData = sessionData,
                ContractorExists = result.ContractorExists
            };

            return View("~/Modules/Invoices/Features/Import/ImportInvoice/Index.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd krytyczny podczas analizy AI: {ex.Message}";
            TempData["ExtractedText"] = extractedText;
            TempData["DocumentType"] = documentType;
            TempData["FilePath"] = filePath;
            return RedirectToAction("Index", "ImportInvoice");
        }
    }

    private string CleanTaxId(string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId)) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var c in taxId)
        {
            if (char.IsDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }
}
