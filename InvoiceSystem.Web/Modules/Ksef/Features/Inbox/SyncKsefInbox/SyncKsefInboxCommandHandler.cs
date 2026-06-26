using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.SyncKsefInbox;

public sealed class SyncKsefInboxCommandHandler(AppDbContext dbContext, IKsefSyncService ksefSyncService, ILogger<SyncKsefInboxCommandHandler> logger) 
    : IRequestHandler<SyncKsefInboxCommand, SyncKsefInboxResult>
{
    public async Task<SyncKsefInboxResult> Handle(SyncKsefInboxCommand request, CancellationToken cancellationToken)
    {
        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            return new SyncKsefInboxResult(false, 0, "Integracja KSeF nie jest poprawnie skonfigurowana.");
        }

        var result = await ksefSyncService.SyncAsync(setting.Id, cancellationToken);
        
        return new SyncKsefInboxResult(result.Success, result.ImportedCount, result.ErrorMessage);
    }
}
