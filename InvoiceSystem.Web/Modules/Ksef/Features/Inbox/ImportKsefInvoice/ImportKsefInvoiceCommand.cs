using MediatR;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.ImportKsefInvoice;

public sealed record ImportKsefInvoiceCommand(int Id) : IRequest<ImportKsefInvoiceResult>;

public sealed record ImportKsefInvoiceResult(bool Success, string? InvoiceNumber, string? SellerName, string? ErrorMessage);
