using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Inbox;

public static class KsefSessionHelper
{
    public static async Task<string> GetValidSessionTokenAsync(AppDbContext dbContext, IKsefClient ksefClient, KsefSetting setting, CancellationToken cancellationToken)
    {
        if (setting.SessionExpiresAt.HasValue && setting.SessionExpiresAt.Value > DateTime.UtcNow && !string.IsNullOrEmpty(setting.ActiveSessionToken))
        {
            return setting.ActiveSessionToken;
        }

        var challenge = await ksefClient.AuthorisationChallengeAsync(setting.Nip, cancellationToken);
        var sessionToken = await ksefClient.InitSessionAsync(
            setting.Nip,
            setting.ApiKey,
            challenge.Challenge,
            challenge.Timestamp,
            cancellationToken
        );

        setting.ActiveSessionToken = sessionToken;
        setting.SessionExpiresAt = DateTime.UtcNow.AddHours(23);
        await dbContext.SaveChangesAsync(cancellationToken);

        return sessionToken;
    }

    public static async Task EnsureRawXmlIsDownloadedAsync(AppDbContext dbContext, IKsefClient ksefClient, KsefIncomingInvoice incoming, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(incoming.RawXml))
        {
            return;
        }

        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting != null && !string.IsNullOrWhiteSpace(setting.Nip) && !string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            var sessionToken = await GetValidSessionTokenAsync(dbContext, ksefClient, setting, cancellationToken);
            var rawXml = await ksefClient.DownloadInvoiceXmlAsync(sessionToken, incoming.KsefNumber, cancellationToken);
            incoming.RawXml = rawXml;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public static string MapExceptionToFriendlyMessage(Exception ex)
    {
        if (ex is KsefApiException kex)
        {
            if (kex.ServiceName == "AuthorisationChallenge")
            {
                if (kex.ServiceCode == "21111" || kex.ServiceCtx.Contains("Nip", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Błąd autoryzacji KSeF: Niepoprawny NIP firmy. Szczegóły: {kex.ServiceCtx}";
                }
                return $"Błąd pobierania wyzwania autoryzacyjnego KSeF: {kex.ServiceCtx}";
            }
            if (kex.ServiceName == "InitSession" && (kex.ServiceCode == "21111" || kex.ServiceCtx.Contains("token", StringComparison.OrdinalIgnoreCase) || kex.ServiceCtx.Contains("uprawni", StringComparison.OrdinalIgnoreCase)))
            {
                return $"Błąd autoryzacji KSeF: Niepoprawny token API lub brak uprawnień. Szczegóły: {kex.ServiceCtx}";
            }
            if (kex.ServiceName == "OpenSession")
            {
                if (kex.ServiceCode == "21470")
                {
                    return $"Błąd otwarcia sesji KSeF: Błąd uwierzytelniania sesji interaktywnej. Szczegóły: {kex.ServiceCtx}";
                }
                return $"Błąd otwarcia sesji KSeF: {kex.ServiceCtx}";
            }
            if (kex.ServiceName == "SyncInvoices")
            {
                return $"Błąd zapytania o metadane faktur KSeF: {kex.ServiceCtx}";
            }
            if (kex.ServiceName == "DownloadInvoiceXml")
            {
                if (kex.ServiceCode == "21164" || kex.ServiceCode == "21165")
                {
                    return $"Błąd KSeF: Nie znaleziono dokumentu o podanym numerze. Szczegóły: {kex.ServiceCtx}";
                }
                return $"Błąd pobierania XML faktury z KSeF: {kex.ServiceCtx}";
            }
            return $"Błąd KSeF [{kex.ServiceCode}] w usłudze '{kex.ServiceName}': {kex.ServiceCtx}";
        }

        if (ex is HttpRequestException hex)
        {
            if (hex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return "Przekroczono limit żądań do KSeF (429). Spróbuj ponownie później.";
            }
            if (hex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                return "Sesja KSeF wygasła (410 Gone). Proszę spróbować ponownie.";
            }
            return $"Błąd komunikacji z KSeF: {hex.Message}";
        }

        return ex.Message;
    }
}
