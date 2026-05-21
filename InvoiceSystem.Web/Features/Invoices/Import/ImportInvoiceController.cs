using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Features.Invoices.Import;

[Route("invoices/import")]
public sealed class ImportInvoiceController(
    IMediator mediator,
    AppDbContext db,
    IMemoryCache cache,
    ILogger<ImportInvoiceController> logger) : Controller
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

            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = $"Analiza AI ({provider}) nie powiodła się: {result.ErrorMessage ?? "Nie udało się sparsować dokumentu."}";
                TempData["ExtractedText"] = extractedText;
                TempData["DocumentType"] = documentType;
                TempData["FilePath"] = filePath;
                return RedirectToAction("Index");
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

            return View("Import", viewModel);
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

    [HttpPost("confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(ConfirmOcrViewModel model, CancellationToken cancellationToken)
    {
        var cacheKey = $"ocr-session-{model.SessionId}";
        if (!cache.TryGetValue<OcrSessionData>(cacheKey, out var sessionData) || sessionData is null)
        {
            TempData["ErrorMessage"] = "Sesja analizy OCR wygasła lub jest nieprawidłowa. Prześlij plik ponownie.";
            return RedirectToAction("Index");
        }

        // Aktualizujemy dane sesji zmianami wprowadzonymi przez operatora w formularzu
        sessionData.BuyerName = model.BuyerName;
        sessionData.BuyerTaxId = CleanTaxId(model.BuyerTaxId);
        sessionData.BuyerAddress = model.BuyerAddress;
        sessionData.Date = model.Date;
        sessionData.Items = model.Items.Select(i => new OcrSessionItem
        {
            Name = i.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        cache.Set(cacheKey, sessionData, TimeSpan.FromMinutes(30));

        // Sprawdzamy czy kontrahent istnieje w bazie
        var cleanTaxId = CleanTaxId(model.BuyerTaxId);
        var contractor = await db.Contractors
            .FirstOrDefaultAsync(c => c.TaxId == cleanTaxId, cancellationToken);

        if (contractor is not null)
        {
            // Kontrahent istnieje - przygotowujemy dane dla formularza i przekierowujemy
            var createCommand = new CreateInvoice.CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = sessionData.Date,
                FilePath = sessionData.FilePath,
                Items = sessionData.Items.Select(i => new CreateInvoice.CreateInvoiceItemCommand(
                    i.Name, i.Quantity, i.UnitPrice)).ToList()
            };

            TempData["AiParsedCommand"] = System.Text.Json.JsonSerializer.Serialize(createCommand);
            cache.Remove(cacheKey);
            
            TempData["SuccessMessage"] = "Dane faktury z analizy AI zostały zaimportowane do formularza.";
            return RedirectToAction("Index", "CreateInvoice");
        }
        else
        {
            // Kontrahent nie istnieje - przekierowujemy do rejestracji kontrahenta
            TempData["WarningMessage"] = $"Brak kontrahenta z NIP: {model.BuyerTaxId} w bazie danych. Musisz zarejestrować go przed utworzeniem faktury.";
            return RedirectToAction("CreateContractor", new { sessionId = model.SessionId });
        }
    }

    [HttpGet("create-contractor")]
    public IActionResult CreateContractor(string sessionId)
    {
        var cacheKey = $"ocr-session-{sessionId}";
        if (!cache.TryGetValue<OcrSessionData>(cacheKey, out var sessionData) || sessionData is null)
        {
            TempData["ErrorMessage"] = "Sesja analizy OCR wygasła lub jest nieprawidłowa.";
            return RedirectToAction("Index");
        }

        ViewBag.SessionId = sessionId;
        ViewBag.WarningMessage = TempData["WarningMessage"] as string;

        var model = new Domain.Entities.Contractor
        {
            Name = sessionData.BuyerName,
            TaxId = sessionData.BuyerTaxId,
            Address = sessionData.BuyerAddress
        };

        return View("ConfirmContractor", model);
    }

    [HttpPost("create-contractor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContractor(string sessionId, Domain.Entities.Contractor model, CancellationToken cancellationToken)
    {
        var cacheKey = $"ocr-session-{sessionId}";
        if (!cache.TryGetValue<OcrSessionData>(cacheKey, out var sessionData) || sessionData is null)
        {
            TempData["ErrorMessage"] = "Sesja analizy OCR wygasła lub jest nieprawidłowa.";
            return RedirectToAction("Index");
        }

        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.TaxId))
        {
            ViewBag.SessionId = sessionId;
            ModelState.AddModelError("", "Nazwa oraz NIP kontrahenta są wymagane.");
            return View("ConfirmContractor", model);
        }

        var cleanTaxId = CleanTaxId(model.TaxId);
        
        // Rozpoczynamy transakcję bazodanową dla zachowania spójności
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Sprawdzamy czy kontrahent nie został przypadkiem dodany w międzyczasie
            var contractor = await db.Contractors
                .FirstOrDefaultAsync(c => c.TaxId == cleanTaxId, cancellationToken);

            if (contractor is null)
            {
                contractor = new Domain.Entities.Contractor
                {
                    Name = model.Name.Trim(),
                    TaxId = cleanTaxId,
                    Address = string.IsNullOrWhiteSpace(model.Address) ? "Brak adresu" : model.Address.Trim()
                };
                db.Contractors.Add(contractor);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Zarejestrowano nowego kontrahenta: {Name} (NIP: {Nip})", contractor.Name, contractor.TaxId);
            }

            await transaction.CommitAsync(cancellationToken);

            // Przygotowujemy dane do formularza z nowym ContractorId
            var createCommand = new CreateInvoice.CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = sessionData.Date,
                FilePath = sessionData.FilePath,
                Items = sessionData.Items.Select(i => new CreateInvoice.CreateInvoiceItemCommand(
                    i.Name, i.Quantity, i.UnitPrice)).ToList()
            };

            TempData["AiParsedCommand"] = System.Text.Json.JsonSerializer.Serialize(createCommand);
            cache.Remove(cacheKey);

            TempData["SuccessMessage"] = "Pomyślnie zarejestrowano nowego kontrahenta. Dane faktury zostały zaimportowane do formularza.";
            return RedirectToAction("Index", "CreateInvoice");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Błąd podczas zapisu rejestracji kontrahenta.");
            ModelState.AddModelError("", $"Błąd zapisu danych: {ex.Message}");
            ViewBag.SessionId = sessionId;
            return View("ConfirmContractor", model);
        }
    }

    [HttpGet("proceed")]
    public IActionResult Proceed(string filePath)
    {
        TempData["UploadedFileName"] = filePath;
        return RedirectToAction("Index", "CreateInvoice");
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
