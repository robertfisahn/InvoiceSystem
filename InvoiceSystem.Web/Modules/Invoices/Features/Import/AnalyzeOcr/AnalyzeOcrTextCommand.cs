using MediatR;
using InvoiceSystem.Web.Modules.Invoices.Infrastructure.Llm;

namespace InvoiceSystem.Web.Modules.Invoices.Features.Import.AnalyzeOcr;

public sealed record AnalyzeOcrTextCommand(string ExtractedText, string Provider) : IRequest<AnalyzeOcrTextResponse>;

public sealed record AnalyzeOcrTextResponse(
    bool Success, 
    LlmInvoiceDto? Data, 
    int? ContractorId = null,
    bool ContractorExists = false,
    string? ErrorMessage = null);
