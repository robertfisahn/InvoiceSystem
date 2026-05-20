namespace InvoiceSystem.Web.Shared.Interfaces;

public sealed record OcrResult(string ExtractedText, string DocumentType, bool Success, string? ErrorMessage = null);

public interface IDocumentOcrService
{
    Task<OcrResult> ExtractTextAsync(string relativePath, CancellationToken cancellationToken);
}
