using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.SyncKsefInbox;

public sealed class SyncKsefInboxCommandHandler(AppDbContext dbContext, IKsefClient ksefClient) 
    : IRequestHandler<SyncKsefInboxCommand, SyncKsefInboxResult>
{
    public async Task<SyncKsefInboxResult> Handle(SyncKsefInboxCommand request, CancellationToken cancellationToken)
    {
        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null || !setting.IsEnabled || string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            return new SyncKsefInboxResult(false, 0, "Integracja KSeF nie jest poprawnie skonfigurowana lub włączona.");
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
                    try
                    {
                        // Wait briefly (250ms) to avoid aggressive API crawling
                        await Task.Delay(250, cancellationToken);

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
                    catch (Exception ex)
                    {
                        // Log individually but continue
                        Console.WriteLine($"Failed to download XML for manual sync of {dto.KsefNumber}: {ex.Message}");
                    }
                }
            }

            // 4. Close session
            await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);

            setting.LastSyncDate = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return new SyncKsefInboxResult(true, newCount, null);
        }
        catch (Exception ex)
        {
            return new SyncKsefInboxResult(false, 0, ex.Message);
        }
    }
}
