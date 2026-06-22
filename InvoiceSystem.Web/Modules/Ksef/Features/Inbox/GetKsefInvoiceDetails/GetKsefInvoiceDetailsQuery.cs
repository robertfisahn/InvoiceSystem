using MediatR;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoiceDetails;

public sealed record GetKsefInvoiceDetailsQuery(int Id) : IRequest<GetKsefInvoiceDetailsResult>;

public sealed record GetKsefInvoiceDetailsResult(
    bool Success,
    int Id,
    string? KsefNumber,
    KsefImportStatus ImportStatus,
    System.DateTime IssueDate,
    ParsedKsefInvoice? ParsedInvoice,
    string? ErrorMessage
);
