using MediatR;
using InvoiceSystem.Web.Modules.Ksef.Domain;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInbox;

public sealed record GetKsefInboxQuery : IRequest<GetKsefInboxResult>;

public sealed record GetKsefInboxResult(
    System.Collections.Generic.List<KsefIncomingInvoice> Invoices,
    bool KsefEnabled,
    bool KsefConfigured
);
