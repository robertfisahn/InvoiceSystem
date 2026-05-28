using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef.DownloadInvoiceKsefUpo;

public sealed class DownloadInvoiceKsefUpoQueryHandler(AppDbContext dbContext) 
    : IRequestHandler<DownloadInvoiceKsefUpoQuery, DownloadInvoiceKsefUpoResult>
{
    public async Task<DownloadInvoiceKsefUpoResult> Handle(DownloadInvoiceKsefUpoQuery request, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice == null)
            return new DownloadInvoiceKsefUpoResult(false, null, null, "Faktura nie istnieje.");

        if (string.IsNullOrEmpty(invoice.UpoXml))
            return new DownloadInvoiceKsefUpoResult(false, null, null, "UPO nie jest jeszcze dostępne dla tej faktury.");

        return new DownloadInvoiceKsefUpoResult(true, invoice.UpoXml, invoice.KsefNumber, null);
    }
}
