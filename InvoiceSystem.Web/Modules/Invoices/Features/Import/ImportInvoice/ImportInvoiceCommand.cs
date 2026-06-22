using MediatR;
using Microsoft.AspNetCore.Http;

namespace InvoiceSystem.Web.Modules.Invoices.Features.Import.ImportInvoice;

public record ImportInvoiceCommand(IFormFile File) : IRequest<ImportInvoiceResponse>;

public record ImportInvoiceResponse(
    bool Success,
    string Message,
    string? FilePath = null,
    string? ExtractedText = null,
    string? DocumentType = null);
