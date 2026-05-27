using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInbox;

public sealed class GetKsefInboxQueryHandler(AppDbContext dbContext) 
    : IRequestHandler<GetKsefInboxQuery, GetKsefInboxResult>
{
    public async Task<GetKsefInboxResult> Handle(GetKsefInboxQuery request, CancellationToken cancellationToken)
    {
        var incomingInvoices = await dbContext.KsefIncomingInvoices
            .AsNoTracking()
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);

        var settings = await dbContext.KsefSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        
        bool ksefEnabled = settings?.IsEnabled ?? false;
        bool ksefConfigured = settings != null && 
                              !string.IsNullOrWhiteSpace(settings.Nip) && 
                              !string.IsNullOrWhiteSpace(settings.ApiKey);

        return new GetKsefInboxResult(incomingInvoices, ksefEnabled, ksefConfigured);
    }
}
