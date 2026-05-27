using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoicePreview;

public sealed class GetKsefInvoicePreviewQueryHandler(AppDbContext dbContext, IKsefClient ksefClient) 
    : IRequestHandler<GetKsefInvoicePreviewQuery, GetKsefInvoicePreviewResult>
{
    public async Task<GetKsefInvoicePreviewResult> Handle(GetKsefInvoicePreviewQuery request, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([request.Id], cancellationToken);
        if (incoming == null)
        {
            return new GetKsefInvoicePreviewResult(false, null, null, null, null, null, null, null, null, 0, [], "Faktura nie istnieje.");
        }

        try
        {
            await KsefSessionHelper.EnsureRawXmlIsDownloadedAsync(dbContext, ksefClient, incoming, cancellationToken);

            if (string.IsNullOrWhiteSpace(incoming.RawXml))
            {
                return new GetKsefInvoicePreviewResult(false, null, null, null, null, null, null, null, null, 0, [], "Brak pobranej zawartości XML faktury oraz brak skonfigurowanego połączenia KSeF do pobrania w locie.");
            }

            var parsed = KsefXmlParser.ParseFa2(incoming.RawXml);
            var items = parsed.Items.Select(i => new GetKsefInvoicePreviewItem(
                i.Name,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice
            )).ToList();

            return new GetKsefInvoicePreviewResult(
                true,
                parsed.InvoiceNumber,
                parsed.Date.ToString("yyyy-MM-dd"),
                parsed.SellerName,
                parsed.SellerNip,
                parsed.SellerAddress,
                parsed.BuyerName,
                parsed.BuyerNip,
                parsed.BuyerAddress,
                parsed.TotalAmount,
                items,
                null
            );
        }
        catch (Exception ex)
        {
            return new GetKsefInvoicePreviewResult(false, null, null, null, null, null, null, null, null, 0, [], ex.Message);
        }
    }
}
