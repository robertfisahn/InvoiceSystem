using MediatR;
using InvoiceSystem.Web.Infrastructure.Services.Llm;

namespace InvoiceSystem.Web.Features.Invoices.Import.AnalyzeOcr;

public sealed record AnalyzeOcrTextCommand(string ExtractedText, string Provider) : IRequest<AnalyzeOcrTextResponse>;

public sealed record AnalyzeOcrTextResponse(
    bool Success, 
    LlmInvoiceDto? Data, 
    int? ContractorId = null,
    bool ContractorExists = false,
    string? ErrorMessage = null);
