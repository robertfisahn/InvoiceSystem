using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.SyncKsefInbox;

public sealed class SyncKsefInboxCommandHandler(AppDbContext dbContext, IKsefClient ksefClient, ILogger<SyncKsefInboxCommandHandler> logger) 
    : IRequestHandler<SyncKsefInboxCommand, SyncKsefInboxResult>
{
    public async Task<SyncKsefInboxResult> Handle(SyncKsefInboxCommand request, CancellationToken cancellationToken)
    {
        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            return new SyncKsefInboxResult(false, 0, "Integracja KSeF nie jest poprawnie skonfigurowana.");
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
            bool hasErrors = false;
            foreach (var dto in incomingInvoices)
            {
                var exists = await dbContext.KsefIncomingInvoices
                    .AnyAsync(i => i.KsefNumber == dto.KsefNumber, cancellationToken);

                if (!exists)
                {
                    try
                    {
                        // Wait 1000ms between calls to avoid aggressive API crawling
                        await Task.Delay(1000, cancellationToken);

                        // Download individual XML content for caching and save it immediately
                        var rawXml = await ksefClient.DownloadInvoiceXmlAsync(sessionToken, dto.KsefNumber, cancellationToken);

                        var newIncoming = new KsefIncomingInvoice
                        {
                            KsefNumber = dto.KsefNumber,
                            SellerName = dto.SellerName,
                            SellerNip = dto.SellerNip,
                            IssueDate = dto.IssueDate,
                            TotalAmount = dto.TotalAmount,
                            RawXml = rawXml,
                            ImportStatus = KsefImportStatus.Pending
                        };
                        dbContext.KsefIncomingInvoices.Add(newIncoming);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        newCount++;
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        logger.LogError(ex, "KSeF API Rate Limit exceeded (429) while syncing invoice {KsefNumber}. Aborting the remaining sync queue to prevent permanent blocking.", dto.KsefNumber);
                        hasErrors = true;
                        break; // abort the queue immediately
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to download XML for manual sync of invoice {KsefNumber}.", dto.KsefNumber);
                        hasErrors = true;
                    }
                }
            }

            // 4. Close session
            await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);

            if (!hasErrors)
            {
                setting.LastSyncDate = DateTime.UtcNow;
            }
            else
            {
                logger.LogWarning("KSeF Sync completed with errors. LastSyncDate was NOT updated to allow retrying missing invoices next time.");
            }
            await dbContext.SaveChangesAsync(cancellationToken);

            if (hasErrors && newCount == 0)
            {
                return new SyncKsefInboxResult(false, 0, "Synchronizacja została przerwana z powodu błędu limitu zapytań (429) lub innego błędu sieciowego. Spróbuj ponownie później.");
            }

            return new SyncKsefInboxResult(true, newCount, hasErrors ? "Niektóre faktury nie mogły zostać pobrane ze względu na limity zapytań API. Zostaną pobrane przy kolejnej synchronizacji." : null);
        }
        catch (Exception ex)
        {
            var friendlyMessage = KsefSessionHelper.MapExceptionToFriendlyMessage(ex);
            return new SyncKsefInboxResult(false, 0, friendlyMessage);
        }
    }
}
