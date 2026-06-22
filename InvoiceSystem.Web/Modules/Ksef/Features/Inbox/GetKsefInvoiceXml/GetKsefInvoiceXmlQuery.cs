using MediatR;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoiceXml;

public sealed record GetKsefInvoiceXmlQuery(int Id) : IRequest<GetKsefInvoiceXmlResult>;

public sealed record GetKsefInvoiceXmlResult(bool Success, string? RawXml, string? ErrorMessage);
