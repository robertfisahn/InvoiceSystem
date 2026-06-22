using System;
using System.Collections.Generic;

namespace InvoiceSystem.Web.Modules.Invoices.Features.Import;

public sealed class ImportInvoiceViewModel
{
    public bool HasResult => !string.IsNullOrEmpty(ExtractedText);
    public string? ExtractedText { get; init; }
    public string? DocumentType { get; init; }
    public string? FilePath { get; init; }
    public string? SuccessMessage { get; init; }
    public string? ErrorMessage { get; init; }
    
    // Dane AI do formularza weryfikacyjnego
    public string? SessionId { get; init; }
    public OcrSessionData? ExtractedData { get; init; }
    public bool ContractorExists { get; init; }
}

public sealed class OcrSessionData
{
    public string FilePath { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerTaxId { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public List<OcrSessionItem> Items { get; set; } = new();
}

public sealed class OcrSessionItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class ConfirmOcrViewModel
{
    public string SessionId { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerTaxId { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public List<ConfirmOcrItemViewModel> Items { get; set; } = new();
}

public sealed class ConfirmOcrItemViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
