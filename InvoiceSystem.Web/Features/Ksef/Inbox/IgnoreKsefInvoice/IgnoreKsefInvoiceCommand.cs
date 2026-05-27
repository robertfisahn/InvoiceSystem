using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.IgnoreKsefInvoice;

public sealed record IgnoreKsefInvoiceCommand(int Id) : IRequest<IgnoreKsefInvoiceResult>;

public sealed record IgnoreKsefInvoiceResult(bool Success, string? ErrorMessage);
