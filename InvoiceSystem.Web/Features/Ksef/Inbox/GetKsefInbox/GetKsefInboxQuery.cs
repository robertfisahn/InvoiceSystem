using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInbox;

public sealed record GetKsefInboxQuery : IRequest<GetKsefInboxResult>;

public sealed record GetKsefInboxResult(
    System.Collections.Generic.List<InvoiceSystem.Web.Domain.Entities.KsefIncomingInvoice> Invoices,
    bool KsefEnabled,
    bool KsefConfigured
);
