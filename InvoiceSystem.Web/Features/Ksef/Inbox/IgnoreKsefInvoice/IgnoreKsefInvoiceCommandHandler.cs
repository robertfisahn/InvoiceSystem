using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.IgnoreKsefInvoice;

public sealed class IgnoreKsefInvoiceCommandHandler(AppDbContext dbContext) 
    : IRequestHandler<IgnoreKsefInvoiceCommand, IgnoreKsefInvoiceResult>
{
    public async Task<IgnoreKsefInvoiceResult> Handle(IgnoreKsefInvoiceCommand request, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([request.Id], cancellationToken);
        if (incoming == null)
        {
            return new IgnoreKsefInvoiceResult(false, "Faktura KSeF nie została znaleziona.");
        }

        incoming.ImportStatus = KsefImportStatus.Ignored;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IgnoreKsefInvoiceResult(true, null);
    }
}
