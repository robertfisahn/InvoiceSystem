using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef;

public sealed record SendInvoiceToKsefCommand(int Id) : IRequest<SendInvoiceToKsefResult>;

public sealed record SendInvoiceToKsefResult(
    bool Success,
    string? KsefNumber,
    string? TransactionId,
    string? ErrorMessage
);
