using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef.DownloadInvoiceKsefXml;

public sealed class DownloadInvoiceKsefXmlQueryHandler(AppDbContext dbContext) 
    : IRequestHandler<DownloadInvoiceKsefXmlQuery, DownloadInvoiceKsefXmlResult>
{
    public async Task<DownloadInvoiceKsefXmlResult> Handle(DownloadInvoiceKsefXmlQuery request, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Contractor)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice == null)
            return new DownloadInvoiceKsefXmlResult(false, null, null, "Faktura nie istnieje.");

        try
        {
            var setting = await dbContext.KsefSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            var sellerNip = setting?.Nip ?? "1234567890";

            var xml = KsefXmlSerializer.SerializeToFa3(invoice, sellerNip);
            return new DownloadInvoiceKsefXmlResult(true, xml, invoice.InvoiceNumber, null);
        }
        catch (Exception ex)
        {
            return new DownloadInvoiceKsefXmlResult(false, null, null, ex.Message);
        }
    }
}
