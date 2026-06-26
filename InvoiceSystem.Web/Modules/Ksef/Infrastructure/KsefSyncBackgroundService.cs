using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Modules.Ksef.Infrastructure;

public sealed class KsefSyncBackgroundService(IServiceProvider serviceProvider, ILogger<KsefSyncBackgroundService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<KsefSyncBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("KSeF Sync Background Service started. Waiting 5 seconds before first sync run.");
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
        var ksefSyncService = scope.ServiceProvider.GetRequiredService<IKsefSyncService>();

        var settings = await dbContext.KsefSettings
            .Where(s => s.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var setting in settings)
        {
            if (string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
            {
                continue;
            }

            _logger.LogInformation("Starting KSeF background synchronization cascade for NIP: {Nip}", setting.Nip);

            bool hasMore = true;
            int cascadeCount = 0;

            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                cascadeCount++;
                _logger.LogInformation("Cascade sync run #{Run} for NIP: {Nip}", cascadeCount, setting.Nip);

                var result = await ksefSyncService.SyncAsync(setting.Id, cancellationToken);

                // Continue cascade only if there is more data AND the last run succeeded (no 429 rate limit hit)
                hasMore = result.HasMore && result.Success;

                if (hasMore)
                {
                    _logger.LogInformation("More invoices remaining on KSeF. Waiting 10 seconds before running next cascade batch for NIP: {Nip}.", setting.Nip);
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("Finished KSeF background synchronization cascade for NIP: {Nip}. Cascade Runs completed: {Runs}", setting.Nip, cascadeCount);
        }
    }
}
