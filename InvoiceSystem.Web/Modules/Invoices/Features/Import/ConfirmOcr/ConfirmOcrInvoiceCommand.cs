using System;
using System.Collections.Generic;
using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.Import.ConfirmOcr;

public sealed record ConfirmOcrInvoiceCommand(
    string SessionId,
    string BuyerName,
    string BuyerTaxId,
    string BuyerAddress,
    DateTime Date,
    List<ConfirmOcrItemCommand> Items
) : IRequest<ConfirmOcrInvoiceResult>;

public sealed record ConfirmOcrItemCommand(
    string Name,
    int Quantity,
    decimal UnitPrice
);

public sealed record ConfirmOcrInvoiceResult(
    bool Success,
    bool ContractorExists,
    string? CreateInvoiceCommandJson,
    string? ErrorMessage
);
