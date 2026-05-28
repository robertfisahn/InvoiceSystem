using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef.DownloadInvoiceKsefXml;

public sealed record DownloadInvoiceKsefXmlQuery(int Id) : IRequest<DownloadInvoiceKsefXmlResult>;

public sealed record DownloadInvoiceKsefXmlResult(
    bool Success,
    string? Xml,
    string? InvoiceNumber,
    string? ErrorMessage
);
