using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceXml;

public sealed class GetKsefInvoiceXmlQueryHandler(AppDbContext dbContext, IKsefClient ksefClient) 
    : IRequestHandler<GetKsefInvoiceXmlQuery, GetKsefInvoiceXmlResult>
{
    public async Task<GetKsefInvoiceXmlResult> Handle(GetKsefInvoiceXmlQuery request, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([request.Id], cancellationToken);
        if (incoming == null)
        {
            return new GetKsefInvoiceXmlResult(false, null, "Faktura KSeF nie została znaleziona.");
        }

        try
        {
            await KsefSessionHelper.EnsureRawXmlIsDownloadedAsync(dbContext, ksefClient, incoming, cancellationToken);

            if (string.IsNullOrWhiteSpace(incoming.RawXml))
            {
                return new GetKsefInvoiceXmlResult(false, null, "Nie udało się pobrać pliku XML z KSeF.");
            }

            return new GetKsefInvoiceXmlResult(true, incoming.RawXml, null);
        }
        catch (Exception ex)
        {
            return new GetKsefInvoiceXmlResult(false, null, ex.Message);
        }
    }
}
