using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Configuration;

[Route("ksef")]
public sealed class KsefConfigurationController(AppDbContext dbContext, IKsefClient ksefClient) : Controller
{
    [HttpGet("config")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null)
        {
            setting = new KsefSetting
            {
                Nip = string.Empty,
                ApiKey = string.Empty,
                IsEnabled = false
            };
            dbContext.KsefSettings.Add(setting);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var viewModel = new KsefConfigurationViewModel
        {
            Nip = setting.Nip,
            ApiKey = setting.ApiKey,
            IsEnabled = setting.IsEnabled,
            ActiveSessionToken = setting.ActiveSessionToken,
            SessionExpiresAt = setting.SessionExpiresAt,
            LastSyncDate = setting.LastSyncDate
        };

        return View(viewModel);
    }

    [HttpPost("config")]
    public async Task<IActionResult> Save(KsefConfigurationViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null)
        {
            setting = new KsefSetting();
            dbContext.KsefSettings.Add(setting);
        }

        setting.Nip = model.Nip;
        setting.ApiKey = model.ApiKey;
        setting.IsEnabled = model.IsEnabled;

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Ustawienia KSeF zostały zapisane.";

        return RedirectToAction("Index");
    }

    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection(string nip, string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nip) || string.IsNullOrWhiteSpace(apiKey))
        {
            return Json(new { success = false, message = "Wprowadź NIP oraz Token Autoryzacyjny." });
        }

        try
        {
            // 1. Get Challenge
            var challenge = await ksefClient.AuthorisationChallengeAsync(nip, cancellationToken);

            // 2. Try initializing session
            var sessionToken = await ksefClient.InitSessionAsync(
                nip,
                apiKey,
                challenge.Challenge,
                challenge.Timestamp,
                cancellationToken
            );

            // 3. Clean up / close test session
            if (!sessionToken.StartsWith("mock-session"))
            {
                await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);
            }

            return Json(new { success = true, message = "Połączenie z Sandboxem KSeF nawiązane pomyślnie!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Błąd połączenia: {ex.Message}" });
        }
    }
}
