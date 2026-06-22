using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;
using InvoiceSystem.Web.Modules.Ksef.Features.Inbox;
using MediatR;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoiceDetails;

public sealed class GetKsefInvoiceDetailsQueryHandler(AppDbContext dbContext, IKsefClient ksefClient) 
    : IRequestHandler<GetKsefInvoiceDetailsQuery, GetKsefInvoiceDetailsResult>
{
    public async Task<GetKsefInvoiceDetailsResult> Handle(GetKsefInvoiceDetailsQuery request, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([request.Id], cancellationToken);
        if (incoming == null)
        {
            return new GetKsefInvoiceDetailsResult(false, 0, null, default, default, null, "Faktura nie istnieje.");
        }

        try
        {
            await KsefSessionHelper.EnsureRawXmlIsDownloadedAsync(dbContext, ksefClient, incoming, cancellationToken);

            if (string.IsNullOrWhiteSpace(incoming.RawXml))
            {
                return new GetKsefInvoiceDetailsResult(false, incoming.Id, incoming.KsefNumber, incoming.ImportStatus, incoming.IssueDate, null, "Brak pliku XML i nie skonfigurowano KSeF do pobrania w locie.");
            }

            var parsed = KsefXmlParser.ParseFa2(incoming.RawXml);

            return new GetKsefInvoiceDetailsResult(
                true,
                incoming.Id,
                incoming.KsefNumber,
                incoming.ImportStatus,
                incoming.IssueDate,
                parsed,
                null
            );
        }
        catch (Exception ex)
        {
            var friendlyMessage = KsefSessionHelper.MapExceptionToFriendlyMessage(ex);
            return new GetKsefInvoiceDetailsResult(false, incoming.Id, incoming.KsefNumber, incoming.ImportStatus, incoming.IssueDate, null, friendlyMessage);
        }
    }
}
