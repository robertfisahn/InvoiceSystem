namespace InvoiceSystem.Web.Features.Invoices.Import;

public sealed class ImportInvoiceViewModel
{
    public bool HasResult => !string.IsNullOrEmpty(ExtractedText);
    public string? ExtractedText { get; init; }
    public string? DocumentType { get; init; }
    public string? FilePath { get; init; }
    public string? SuccessMessage { get; init; }
    public string? ErrorMessage { get; init; }
}
