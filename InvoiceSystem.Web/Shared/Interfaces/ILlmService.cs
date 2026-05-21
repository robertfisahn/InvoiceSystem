using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceSystem.Web.Shared.Interfaces;

public record LlmInvoiceDto(
    string BuyerName,
    string BuyerTaxId,
    string BuyerAddress,
    string InvoiceNumber,
    DateTime? Date,
    List<LlmInvoiceItemDto> Items
);

public record LlmInvoiceItemDto(
    string Name,
    decimal Quantity,
    decimal UnitPrice
);

public interface ILlmService
{
    Task<LlmInvoiceDto?> ExtractInvoiceDataAsync(string ocrText, string provider, CancellationToken cancellationToken);
}
