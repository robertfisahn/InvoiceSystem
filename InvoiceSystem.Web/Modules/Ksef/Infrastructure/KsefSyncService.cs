using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Modules.Ksef.Infrastructure
{
    public record KsefSyncResult(bool Success, int ImportedCount, bool HasMore, string? ErrorMessage);

    public interface IKsefSyncService
    {
        Task<KsefSyncResult> SyncAsync(int settingsId, CancellationToken cancellationToken);
    }

    public sealed class KsefSyncService : IKsefSyncService
    {
        private readonly IKsefSyncLock _syncLock;
        private readonly AppDbContext _dbContext;
        private readonly IKsefClient _ksefClient;
        private readonly ILogger<KsefSyncService> _logger;

        public KsefSyncService(IKsefSyncLock syncLock, AppDbContext dbContext, IKsefClient ksefClient, ILogger<KsefSyncService> logger)
        {
            _syncLock = syncLock;
            _dbContext = dbContext;
            _ksefClient = ksefClient;
            _logger = logger;
        }

        public async Task<KsefSyncResult> SyncAsync(int settingsId, CancellationToken cancellationToken)
        {
            // Concurrency Lock: Try to acquire lock immediately (timeout 0)
            bool acquired = await _syncLock.TryAcquireAsync(cancellationToken);
            if (!acquired)
            {
                _logger.LogWarning("Synchronization skipped. Another KSeF sync is already in progress.");
                return new KsefSyncResult(false, 0, false, "Trwa automatyczna synchronizacja faktur w tle lub inny użytkownik wywołał synchronizację. Spróbuj ponownie za chwilę.");
            }

            try
            {
                return await ExecuteSyncInternalAsync(settingsId, cancellationToken);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private async Task<KsefSyncResult> ExecuteSyncInternalAsync(int settingsId, CancellationToken cancellationToken)
        {
            var setting = await _dbContext.KsefSettings.FirstOrDefaultAsync(s => s.Id == settingsId, cancellationToken);
            if (setting == null || string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
            {
                return new KsefSyncResult(false, 0, false, "Konfiguracja integracji KSeF jest niepoprawna.");
            }

            string? sessionToken = null;
            try
            {
                _logger.LogInformation("Initiating KSeF session for NIP: {Nip}", setting.Nip);

                // 1. Authorisation Challenge
                var challenge = await _ksefClient.AuthorisationChallengeAsync(setting.Nip, cancellationToken);

                // 2. Init session
                sessionToken = await _ksefClient.InitSessionAsync(
                    setting.Nip,
                    setting.ApiKey,
                    challenge.Challenge,
                    challenge.Timestamp,
                    cancellationToken
                );

                setting.ActiveSessionToken = sessionToken;
                setting.SessionExpiresAt = DateTime.UtcNow.AddHours(23);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // 3. Retrieve all new invoices metadata since last sync watermark
                var syncFrom = setting.LastSyncDate ?? DateTime.UtcNow.AddDays(-30);
                var incomingInvoices = await _ksefClient.SyncInvoicesAsync(sessionToken, syncFrom, cancellationToken);

                // Filter invoices that do not exist yet in the database
                var pendingInvoices = new List<KsefIncomingInvoiceDto>();
                foreach (var dto in incomingInvoices)
                {
                    var exists = await _dbContext.KsefIncomingInvoices
                        .AnyAsync(i => i.KsefNumber == dto.KsefNumber, cancellationToken);
                    if (!exists)
                    {
                        pendingInvoices.Add(dto);
                    }
                }

                // Sort by IssueDate ascending so that LastSyncDate increments forward chronologically
                pendingInvoices = pendingInvoices.OrderBy(dto => dto.IssueDate).ToList();

                // Take max 10 invoices for this batch
                const int BatchSize = 10;
                var batchToProcess = pendingInvoices.Take(BatchSize).ToList();
                bool hasMore = pendingInvoices.Count > BatchSize;

                int importedCount = 0;
                bool hasErrors = false;
                string? loopErrorMessage = null;

                foreach (var dto in batchToProcess)
                {
                    try
                    {
                        // Cooldown delay between single calls (1500ms as per policy)
                        await Task.Delay(1500, cancellationToken);

                        _logger.LogInformation("Downloading XML for KSeF invoice: {KsefNumber}", dto.KsefNumber);
                        var rawXml = await _ksefClient.DownloadInvoiceXmlAsync(sessionToken, dto.KsefNumber, cancellationToken);

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
                        _dbContext.KsefIncomingInvoices.Add(newIncoming);

                        // Incremental Watermarking: update watermark to the processed invoice's IssueDate
                        setting.LastSyncDate = dto.IssueDate;

                        // Save current invoice and updated watermark together in one atomic transaction
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        importedCount++;
                        _logger.LogInformation("Successfully imported KSeF invoice: {KsefNumber}", dto.KsefNumber);
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogError(ex, "KSeF API Rate Limit exceeded (429) during sync of invoice {KsefNumber}. Aborting current batch loop.", dto.KsefNumber);
                        hasErrors = true;
                        loopErrorMessage = "Przekroczono limit zapytań KSeF (429). Pobieranie kolejnych faktur zostało wstrzymane.";
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to download/import invoice {KsefNumber}.", dto.KsefNumber);
                        hasErrors = true;
                        loopErrorMessage = "Wystąpił błąd podczas pobierania faktury z KSeF.";
                    }
                }

                // Poll outgoing pending invoices
                var pendingOutgoing = await _dbContext.Invoices
                    .Where(i => i.KsefTransactionId != null && i.KsefNumber == null)
                    .ToListAsync(cancellationToken);

                foreach (var inv in pendingOutgoing)
                {
                    if (string.IsNullOrEmpty(inv.KsefTransactionId)) continue;
                    try
                    {
                        await Task.Delay(1000, cancellationToken); // be gentle
                        var statusResult = await _ksefClient.GetInvoiceStatusAsync(sessionToken, inv.KsefTransactionId, cancellationToken);
                        if (statusResult.Status == "Processed" && !string.IsNullOrEmpty(statusResult.KsefNumber))
                        {
                            inv.KsefNumber = statusResult.KsefNumber;
                            var upoXml = await _ksefClient.DownloadUpoAsync(sessionToken, statusResult.KsefNumber, cancellationToken);
                            inv.UpoXml = upoXml;
                            await _dbContext.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation("Outgoing invoice {InvoiceNumber} processed by KSeF. Assigned number: {KsefNumber}", inv.InvoiceNumber, statusResult.KsefNumber);
                        }
                        else if (statusResult.Status == "Failed")
                        {
                            _logger.LogWarning("Outgoing invoice {InvoiceNumber} rejected by KSeF: {Msg}", inv.InvoiceNumber, statusResult.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to poll status for transaction {TxId} of invoice {InvoiceNumber}", inv.KsefTransactionId, inv.InvoiceNumber);
                    }
                }

                return new KsefSyncResult(!hasErrors, importedCount, hasMore, loopErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KSeF synchronization session failed.");
                return new KsefSyncResult(false, 0, false, Features.Inbox.KsefSessionHelper.MapExceptionToFriendlyMessage(ex));
            }
            finally
            {
                if (sessionToken != null)
                {
                    try
                    {
                        await _ksefClient.CloseSessionAsync(sessionToken, CancellationToken.None);
                        
                        // Clear active session in DB since it is closed
                        var freshSetting = await _dbContext.KsefSettings.FirstOrDefaultAsync(s => s.Id == settingsId, cancellationToken);
                        if (freshSetting != null)
                        {
                            freshSetting.ActiveSessionToken = null;
                            freshSetting.SessionExpiresAt = null;
                            await _dbContext.SaveChangesAsync(CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to close KSeF session gracefully.");
                    }
                }
            }
        }
    }
}
