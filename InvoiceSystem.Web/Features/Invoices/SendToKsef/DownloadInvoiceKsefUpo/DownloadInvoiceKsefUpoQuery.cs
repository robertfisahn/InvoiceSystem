using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef.DownloadInvoiceKsefUpo;

public sealed record DownloadInvoiceKsefUpoQuery(int Id) : IRequest<DownloadInvoiceKsefUpoResult>;

public sealed record DownloadInvoiceKsefUpoResult(
    bool Success,
    string? UpoXml,
    string? KsefNumber,
    string? ErrorMessage
);
