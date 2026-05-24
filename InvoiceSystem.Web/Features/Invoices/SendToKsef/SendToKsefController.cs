using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef;

[Route("invoices")]
public sealed class SendToKsefController(AppDbContext dbContext, IKsefClient ksefClient) : Controller
{
    [HttpPost("{id:int}/send-to-ksef")]
    public async Task<IActionResult> Send(int id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Contractor)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice == null)
        {
            TempData["ErrorMessage"] = "Faktura nie istnieje.";
            return RedirectToAction("Index", "GetInvoiceDetails", new { id });
        }

        if (invoice.Status != InvoiceStatus.Confirmed)
        {
            TempData["ErrorMessage"] = "Tylko zatwierdzone faktury mogą być wysłane do KSeF.";
            return RedirectToAction("Index", "GetInvoiceDetails", new { id });
        }

        if (!string.IsNullOrEmpty(invoice.KsefNumber))
        {
            TempData["ErrorMessage"] = "Ta faktura posiada już nadany numer KSeF.";
            return RedirectToAction("Index", "GetInvoiceDetails", new { id });
        }

        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null || !setting.IsEnabled || string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            TempData["ErrorMessage"] = "Integracja z KSeF jest wyłączona lub nieskonfigurowana. Skonfiguruj NIP i Token w ustawieniach.";
            return RedirectToAction("Index", "GetInvoiceDetails", new { id });
        }

        try
        {
            // 1. Generate XML
            var xmlContent = KsefXmlSerializer.SerializeToFa2(invoice);

            // 2. Authorise Session
            var challenge = await ksefClient.AuthorisationChallengeAsync(setting.Nip, cancellationToken);
            var sessionToken = await ksefClient.InitSessionAsync(
                setting.Nip,
                setting.ApiKey,
                challenge.Challenge,
                challenge.Timestamp,
                cancellationToken
            );

            // 3. Send
            var transactionId = await ksefClient.SendInvoiceAsync(sessionToken, xmlContent, cancellationToken);
            
            invoice.KsefTransactionId = transactionId;
            invoice.KsefSentAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            // 4. In Mock Mode (or fast response), immediately check status
            var statusResult = await ksefClient.GetInvoiceStatusAsync(sessionToken, transactionId, cancellationToken);
            if (statusResult.Status == "Processed" && !string.IsNullOrEmpty(statusResult.KsefNumber))
            {
                invoice.KsefNumber = statusResult.KsefNumber;
                
                // Get UPO
                var upoXml = await ksefClient.DownloadUpoAsync(sessionToken, statusResult.KsefNumber, cancellationToken);
                invoice.UpoXml = upoXml;

                await dbContext.SaveChangesAsync(cancellationToken);
                TempData["SuccessMessage"] = $"Faktura wysłana do KSeF. Nadano numer KSeF: {statusResult.KsefNumber} i wygenerowano UPO.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Faktura przekazana do KSeF. ID transakcji: {transactionId}. Oczekuje na przetworzenie.";
            }

            // 5. Close session
            if (!sessionToken.StartsWith("mock-session"))
            {
                await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Błąd podczas wysyłki do KSeF: {ex.Message}";
        }

        return RedirectToAction("Index", "GetInvoiceDetails", new { id });
    }

    [HttpGet("{id:int}/ksef-xml")]
    public async Task<IActionResult> DownloadXml(int id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Contractor)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (invoice == null)
            return NotFound();

        var xml = KsefXmlSerializer.SerializeToFa2(invoice);
        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        return File(bytes, "application/xml", $"KSEF-{invoice.InvoiceNumber}.xml");
    }

    [HttpGet("{id:int}/ksef-upo")]
    public async Task<IActionResult> DownloadUpo(int id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.FindAsync([id], cancellationToken);
        if (invoice == null || string.IsNullOrEmpty(invoice.UpoXml))
            return NotFound("UPO nie jest jeszcze dostępne dla tej faktury.");

        var bytes = System.Text.Encoding.UTF8.GetBytes(invoice.UpoXml);
        return File(bytes, "application/xml", $"UPO-{invoice.KsefNumber}.xml");
    }
}
