using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.SendToKsef.SendInvoiceToKsef;

public sealed record SendInvoiceToKsefCommand(int Id) : IRequest<SendInvoiceToKsefResult>;

public sealed record SendInvoiceToKsefResult(
    bool Success,
    string? KsefNumber,
    string? TransactionId,
    string? ErrorMessage
);
