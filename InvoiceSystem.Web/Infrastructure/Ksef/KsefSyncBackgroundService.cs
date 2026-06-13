using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Infrastructure.Ksef;

public sealed class KsefSyncBackgroundService(IServiceProvider serviceProvider, ILogger<KsefSyncBackgroundService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<KsefSyncBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("KSeF Sync Background Service started. Waiting 1 minute before first sync run.");
        }
        catch {}

        // Wait 5 seconds before first sync to speed up developer feedback loop
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllActiveConfigsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogError(ex, "Error occurred during KSeF synchronization.");
                }
                catch {}
            }

            try
            {
                // Sync every 5 minutes in development for highly responsive behavior
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncAllActiveConfigsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ksefClient = scope.ServiceProvider.GetRequiredService<IKsefClient>();

        var settings = await dbContext.KsefSettings
            .Where(s => s.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            if (string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
            {
                continue;
            }

            _logger.LogInformation("Starting KSeF synchronization for NIP: {Nip}", setting.Nip);

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

                // Update setting session details (session token lasts up to 24h)
                setting.ActiveSessionToken = sessionToken;
                setting.SessionExpiresAt = DateTime.UtcNow.AddHours(23); // expire early

                // 3. Sync invoices (default from last sync date or 30 days ago)
                var syncFrom = setting.LastSyncDate ?? DateTime.UtcNow.AddDays(-30);
                var incomingInvoices = await ksefClient.SyncInvoicesAsync(sessionToken, syncFrom, cancellationToken);

                int newCount = 0;
                bool hasErrors = false;
                foreach (var dto in incomingInvoices)
                {
                    // Avoid importing duplicates
                    var exists = await dbContext.KsefIncomingInvoices
                        .AnyAsync(i => i.KsefNumber == dto.KsefNumber, cancellationToken);

                    if (!exists)
                    {
                        try
                        {
                            // Wait 1000ms between calls to avoid aggressive crawling
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
                            _logger.LogInformation("Successfully imported new KSeF invoice: {KsefNumber}", dto.KsefNumber);
                        }
                        catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogError(ex, "KSeF API Rate Limit exceeded (429) during background sync while syncing invoice {KsefNumber}. Aborting the remaining sync queue to prevent permanent blocking.", dto.KsefNumber);
                            hasErrors = true;
                            break; // abort the queue immediately
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                _logger.LogError(ex, "Failed to download XML or save incoming invoice {KsefNumber}", dto.KsefNumber);
                            }
                            catch {}
                            hasErrors = true;
                        }
                    }
                }

                // Sync/Poll outgoing pending invoices
                var pendingInvoices = await dbContext.Invoices
                    .Where(i => i.KsefTransactionId != null && i.KsefNumber == null)
                    .ToListAsync(cancellationToken);

                foreach (var inv in pendingInvoices)
                {
                    if (string.IsNullOrEmpty(inv.KsefTransactionId)) continue;
                    try
                    {
                        var statusResult = await ksefClient.GetInvoiceStatusAsync(sessionToken, inv.KsefTransactionId, cancellationToken);
                        if (statusResult.Status == "Processed" && !string.IsNullOrEmpty(statusResult.KsefNumber))
                        {
                            inv.KsefNumber = statusResult.KsefNumber;
                            var upoXml = await ksefClient.DownloadUpoAsync(sessionToken, statusResult.KsefNumber, cancellationToken);
                            inv.UpoXml = upoXml;
                            _logger.LogInformation("Outgoing invoice {InvoiceNumber} processed by KSeF. Assigned number: {KsefNumber}", inv.InvoiceNumber, statusResult.KsefNumber);
                        }
                        else if (statusResult.Status == "Failed")
                        {
                            _logger.LogWarning("Outgoing invoice {InvoiceNumber} rejected by KSeF: {Msg}", inv.InvoiceNumber, statusResult.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            _logger.LogError(ex, "Failed to poll status for transaction {TxId} of invoice {InvoiceNumber}", inv.KsefTransactionId, inv.InvoiceNumber);
                        }
                        catch {}
                    }
                }

                // 4. Close session
                await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);

                if (!hasErrors)
                {
                    setting.LastSyncDate = DateTime.UtcNow;
                    _logger.LogInformation("KSeF sync complete for NIP: {Nip}. Added {Count} new invoices.", setting.Nip, newCount);
                }
                else
                {
                    _logger.LogWarning("KSeF sync completed with errors or was aborted for NIP: {Nip}. Added {Count} new invoices. LastSyncDate was NOT updated.", setting.Nip, newCount);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogError(ex, "Failed to synchronize KSeF for NIP: {Nip}", setting.Nip);
                }
                catch {}
            }
        }
    }
}
