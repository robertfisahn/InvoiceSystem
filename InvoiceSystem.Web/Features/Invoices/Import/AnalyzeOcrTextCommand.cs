using MediatR;
using InvoiceSystem.Web.Shared.Interfaces;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public sealed record AnalyzeOcrTextCommand(string ExtractedText, string Provider) : IRequest<AnalyzeOcrTextResponse>;

public sealed record AnalyzeOcrTextResponse(
    bool Success, 
    LlmInvoiceDto? Data, 
    int? ContractorId = null,
    bool ContractorExists = false,
    string? ErrorMessage = null);
