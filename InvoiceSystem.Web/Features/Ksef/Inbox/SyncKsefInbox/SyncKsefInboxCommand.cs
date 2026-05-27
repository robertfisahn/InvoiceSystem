using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.SyncKsefInbox;

public sealed record SyncKsefInboxCommand : IRequest<SyncKsefInboxResult>;

public sealed record SyncKsefInboxResult(bool Success, int NewInvoicesCount, string? ErrorMessage);
