using MediatR;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoicePreview;

public sealed record GetKsefInvoicePreviewQuery(int Id) : IRequest<GetKsefInvoicePreviewResult>;

public sealed record GetKsefInvoicePreviewResult(
    bool Success,
    string? InvoiceNumber,
    string? Date,
    string? SellerName,
    string? SellerNip,
    string? SellerAddress,
    string? BuyerName,
    string? BuyerNip,
    string? BuyerAddress,
    decimal TotalAmount,
    System.Collections.Generic.List<GetKsefInvoicePreviewItem> Items,
    string? ErrorMessage
);

public sealed record GetKsefInvoicePreviewItem(
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);
