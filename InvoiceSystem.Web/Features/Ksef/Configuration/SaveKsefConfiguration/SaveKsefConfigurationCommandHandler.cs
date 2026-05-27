using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.SaveKsefConfiguration;

public sealed class SaveKsefConfigurationCommandHandler(AppDbContext dbContext) 
    : IRequestHandler<SaveKsefConfigurationCommand, Unit>
{
    public async Task<Unit> Handle(SaveKsefConfigurationCommand request, CancellationToken cancellationToken)
    {
        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null)
        {
            setting = new KsefSetting();
            dbContext.KsefSettings.Add(setting);
        }

        setting.Nip = request.Model.Nip;
        setting.ApiKey = request.Model.ApiKey;
        setting.IsEnabled = request.Model.IsEnabled;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
