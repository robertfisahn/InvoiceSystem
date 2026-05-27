using MediatR;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Ksef;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceDetails;

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
