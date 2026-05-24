using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Inbox;

[Route("ksef/inbox")]
public sealed class KsefInboxController(AppDbContext dbContext, IKsefClient ksefClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var incomingInvoices = await dbContext.KsefIncomingInvoices
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);

        var settings = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        ViewBag.KsefEnabled = settings?.IsEnabled ?? false;
        ViewBag.KsefConfigured = settings != null && !string.IsNullOrWhiteSpace(settings.Nip) && !string.IsNullOrWhiteSpace(settings.ApiKey);

        return View(incomingInvoices);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null || !setting.IsEnabled || string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            TempData["ErrorMessage"] = "Integracja KSeF nie jest poprawnie skonfigurowana lub włączona.";
            return RedirectToAction("Index");
        }

        try
        {
            // 1. Authorisation Challenge
            var challenge = await ksefClient.AuthorisationChallengeAsync(setting.Nip, cancellationToken);

            // 2. Init session
            var sessionToken = await ksefClient.InitSessionAsync(
                setting.Nip,
                setting.ApiKey,
                challenge.Challenge,
                challenge.Timestamp,
                cancellationToken
            );

            setting.ActiveSessionToken = sessionToken;
            setting.SessionExpiresAt = DateTime.UtcNow.AddHours(23);

            // 3. Sync
            var syncFrom = setting.LastSyncDate ?? DateTime.UtcNow.AddDays(-30);
            var incomingInvoices = await ksefClient.SyncInvoicesAsync(sessionToken, syncFrom, cancellationToken);

            int newCount = 0;
            foreach (var dto in incomingInvoices)
            {
                var exists = await dbContext.KsefIncomingInvoices
                    .AnyAsync(i => i.KsefNumber == dto.KsefNumber, cancellationToken);

                if (!exists)
                {
                    var newIncoming = new KsefIncomingInvoice
                    {
                        KsefNumber = dto.KsefNumber,
                        SellerName = dto.SellerName,
                        SellerNip = dto.SellerNip,
                        IssueDate = dto.IssueDate,
                        TotalAmount = dto.TotalAmount,
                        RawXml = dto.RawXml,
                        ImportStatus = KsefImportStatus.Pending
                    };
                    dbContext.KsefIncomingInvoices.Add(newIncoming);
                    newCount++;
                }
            }

            // 4. Close session
            if (!sessionToken.StartsWith("mock-session"))
            {
                await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);
            }

            setting.LastSyncDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            TempData["SuccessMessage"] = $"Synchronizacja zakończona pomyślnie. Pobrano {newCount} nowych faktur kosztowych.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd synchronizacji: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    [HttpPost("import/{id:int}")]
    public async Task<IActionResult> Import(int id, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([id], cancellationToken);
        if (incoming == null)
        {
            TempData["ErrorMessage"] = "Faktura KSeF nie została znaleziona.";
            return RedirectToAction("Index");
        }

        if (incoming.ImportStatus != KsefImportStatus.Pending)
        {
            TempData["ErrorMessage"] = "Faktura została już zaimportowana lub zignorowana.";
            return RedirectToAction("Index");
        }

        try
        {
            // Parse XML to extract invoice details
            var parsed = KsefXmlParser.ParseFa2(incoming.RawXml);

            // Start transaction for atomic execution
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Find or create Contractor (Seller)
            var contractor = await dbContext.Contractors
                .FirstOrDefaultAsync(c => c.TaxId == parsed.SellerNip, cancellationToken);

            if (contractor == null)
            {
                contractor = new Contractor
                {
                    Name = parsed.SellerName,
                    TaxId = parsed.SellerNip,
                    Address = parsed.SellerAddress
                };
                dbContext.Contractors.Add(contractor);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // Create system Invoice as a confirmed purchase invoice
            var newInvoice = new Invoice
            {
                InvoiceNumber = parsed.InvoiceNumber,
                Date = parsed.Date,
                ContractorId = contractor.Id,
                Status = InvoiceStatus.Confirmed,
                KsefNumber = incoming.KsefNumber,
                KsefSentAt = incoming.IssueDate
            };

            dbContext.Invoices.Add(newInvoice);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Map and add items
            foreach (var item in parsed.Items)
            {
                var invoiceItem = new InvoiceItem
                {
                    InvoiceId = newInvoice.Id,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                dbContext.InvoiceItems.Add(invoiceItem);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Update buffer import status
            incoming.ImportStatus = KsefImportStatus.Imported;
            incoming.ImportedInvoiceId = newInvoice.Id;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            TempData["SuccessMessage"] = $"Faktura {parsed.InvoiceNumber} od {parsed.SellerName} została zaimportowana do systemu.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Wystąpił błąd podczas importowania: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    [HttpPost("ignore/{id:int}")]
    public async Task<IActionResult> Ignore(int id, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([id], cancellationToken);
        if (incoming == null)
        {
            TempData["ErrorMessage"] = "Faktura KSeF nie została znaleziona.";
            return RedirectToAction("Index");
        }

        incoming.ImportStatus = KsefImportStatus.Ignored;
        await dbContext.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Faktura została oznaczona jako zignorowana.";
        return RedirectToAction("Index");
    }

    [HttpGet("preview/{id:int}")]
    public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([id], cancellationToken);
        if (incoming == null)
        {
            return NotFound("Faktura nie istnieje.");
        }

        try
        {
            var parsed = KsefXmlParser.ParseFa2(incoming.RawXml);
            return Json(new
            {
                success = true,
                invoiceNumber = parsed.InvoiceNumber,
                date = parsed.Date.ToString("yyyy-MM-dd"),
                sellerName = parsed.SellerName,
                sellerNip = parsed.SellerNip,
                sellerAddress = parsed.SellerAddress,
                totalAmount = parsed.TotalAmount,
                items = parsed.Items.Select(i => new
                {
                    name = i.Name,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    totalPrice = i.TotalPrice
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("xml/{id:int}")]
    public async Task<IActionResult> GetXml(int id, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([id], cancellationToken);
        if (incoming == null)
        {
            return NotFound();
        }

        return Content(incoming.RawXml, "application/xml");
    }
}
