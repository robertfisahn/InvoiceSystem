using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.GetKsefConfiguration;

public sealed class GetKsefConfigurationQueryHandler(AppDbContext dbContext) 
    : IRequestHandler<GetKsefConfigurationQuery, KsefConfigurationViewModel>
{
    public async Task<KsefConfigurationViewModel> Handle(GetKsefConfigurationQuery request, CancellationToken cancellationToken)
    {
        var setting = await dbContext.KsefSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (setting == null)
        {
            return new KsefConfigurationViewModel
            {
                Nip = string.Empty,
                ApiKey = string.Empty,
                IsEnabled = false
            };
        }

        return new KsefConfigurationViewModel
        {
            Nip = setting.Nip,
            ApiKey = setting.ApiKey,
            IsEnabled = setting.IsEnabled,
            ActiveSessionToken = setting.ActiveSessionToken,
            SessionExpiresAt = setting.SessionExpiresAt,
            LastSyncDate = setting.LastSyncDate
        };
    }
}
