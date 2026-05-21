using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.Import;

[Route("invoices/import")]
public sealed class ImportInvoiceController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var viewModel = new ImportInvoiceViewModel
        {
            ErrorMessage = TempData["ErrorMessage"] as string,
            SuccessMessage = TempData["SuccessMessage"] as string,
            ExtractedText = TempData["ExtractedText"] as string,
            DocumentType = TempData["DocumentType"] as string,
            FilePath = TempData["FilePath"] as string
        };

        return View("Import", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile file)
    {
        try
        {
            var result = await mediator.Send(new ImportInvoiceCommand(file));

            var viewModel = new ImportInvoiceViewModel
            {
                SuccessMessage = result.Success ? result.Message : null,
                ErrorMessage = result.Success ? null : result.Message,
                ExtractedText = result.ExtractedText,
                DocumentType = result.DocumentType,
                FilePath = result.FilePath
            };

            return View("Import", viewModel);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var viewModel = new ImportInvoiceViewModel
            {
                ErrorMessage = string.Join("<br/>", ex.Errors.Select(e => e.ErrorMessage))
            };

            return View("Import", viewModel);
        }
    }

    [HttpPost("analyze")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(string extractedText, string provider, string filePath, string documentType)
    {
        try
        {
            var result = await mediator.Send(new AnalyzeOcrTextCommand(extractedText, provider));

            if (!result.Success || result.Data is null || !result.ContractorId.HasValue)
            {
                TempData["ErrorMessage"] = $"Analiza AI ({provider}) nie powiodła się: {result.ErrorMessage ?? "Nie udało się sparsować dokumentu."}";
                TempData["ExtractedText"] = extractedText;
                TempData["DocumentType"] = documentType;
                TempData["FilePath"] = filePath;
                return RedirectToAction("Index");
            }

            var dto = result.Data;

            // Tworzymy CreateInvoiceCommand na podstawie danych z AI
            var createCommand = new CreateInvoice.CreateInvoiceCommand
            {
                ContractorId = result.ContractorId.Value,
                Date = dto.Date ?? DateTime.Today,
                FilePath = filePath,
                Items = dto.Items.Select(i => new CreateInvoice.CreateInvoiceItemCommand(
                    i.Name ?? "Pozycja z OCR", 
                    i.Quantity <= 0 ? 1 : i.Quantity, 
                    i.UnitPrice < 0 ? 0 : i.UnitPrice)).ToList()
            };

            TempData["AiParsedCommand"] = System.Text.Json.JsonSerializer.Serialize(createCommand);
            return RedirectToAction("Index", "CreateInvoice");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd krytyczny podczas analizy AI: {ex.Message}";
            TempData["ExtractedText"] = extractedText;
            TempData["DocumentType"] = documentType;
            TempData["FilePath"] = filePath;
            return RedirectToAction("Index");
        }
    }

    [HttpGet("proceed")]
    public IActionResult Proceed(string filePath)
    {
        TempData["UploadedFileName"] = filePath;
        return RedirectToAction("Index", "CreateInvoice");
    }
}
