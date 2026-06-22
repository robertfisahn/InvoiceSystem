using MediatR;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.IgnoreKsefInvoice;

public sealed record IgnoreKsefInvoiceCommand(int Id) : IRequest<IgnoreKsefInvoiceResult>;

public sealed record IgnoreKsefInvoiceResult(bool Success, string? ErrorMessage);
