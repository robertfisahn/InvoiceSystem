using System;
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
}
