using MediatR;
using Microsoft.AspNetCore.Http;

namespace InvoiceSystem.Web.Features.Invoices.Import.ImportInvoice;

public record ImportInvoiceCommand(IFormFile File) : IRequest<ImportInvoiceResponse>;

public record ImportInvoiceResponse(
    bool Success,
    string Message,
    string? FilePath = null,
    string? ExtractedText = null,
    string? DocumentType = null);
