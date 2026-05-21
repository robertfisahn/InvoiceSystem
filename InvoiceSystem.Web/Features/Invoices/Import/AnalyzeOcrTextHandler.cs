using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Shared.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public sealed class AnalyzeOcrTextHandler(
    ILlmService llmService,
    AppDbContext db,
    ILogger<AnalyzeOcrTextHandler> logger) : IRequestHandler<AnalyzeOcrTextCommand, AnalyzeOcrTextResponse>
{
    public async Task<AnalyzeOcrTextResponse> Handle(AnalyzeOcrTextCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Rozpoczynanie przetwarzania tekstu OCR za pomocą: {Provider}", request.Provider);

            var extractedData = await llmService.ExtractInvoiceDataAsync(request.ExtractedText, request.Provider, cancellationToken);
            if (extractedData is null)
            {
                logger.LogWarning("Ekstrakcja danych z tekstu przez LLM nie zwróciła wyniku.");
                return new AnalyzeOcrTextResponse(false, null, null, "Nie udało się poprawnie sparsować faktury. Upewnij się, że tekst zawiera poprawne dane faktury.");
            }

            // 1. Sprawdzanie i ewentualne tworzenie kontrahenta
            var cleanTaxId = CleanTaxId(extractedData.SellerTaxId);
            if (string.IsNullOrWhiteSpace(cleanTaxId))
            {
                logger.LogWarning("AI nie odnalazło poprawnego NIP sprzedawcy.");
                return new AnalyzeOcrTextResponse(false, extractedData, null, "NIP sprzedawcy jest wymagany do identyfikacji i przypisania kontrahenta w systemie.");
            }

            var contractor = await db.Contractors
                .FirstOrDefaultAsync(c => c.TaxId == cleanTaxId, cancellationToken);

            if (contractor is null)
            {
                logger.LogInformation("Kontrahent o NIP {Nip} nie został odnaleziony. Tworzenie nowej kartoteki...", cleanTaxId);
                contractor = new Contractor
                {
                    Name = string.IsNullOrWhiteSpace(extractedData.SellerName) ? "Kontrahent z OCR" : extractedData.SellerName.Trim(),
                    TaxId = cleanTaxId,
                    Address = string.IsNullOrWhiteSpace(extractedData.SellerAddress) ? "Brak adresu" : extractedData.SellerAddress.Trim()
                };

                db.Contractors.Add(contractor);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Pomyślnie dodano nowego kontrahenta: {Name} (ID: {Id})", contractor.Name, contractor.Id);
            }
            else
            {
                logger.LogInformation("Znaleziono istniejącego kontrahenta: {Name} (ID: {Id})", contractor.Name, contractor.Id);
            }

            return new AnalyzeOcrTextResponse(true, extractedData, contractor.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Wystąpił nieoczekiwany błąd w AnalyzeOcrTextHandler.");
            return new AnalyzeOcrTextResponse(false, null, null, $"Błąd serwera analizy: {ex.Message}");
        }
    }

    private string CleanTaxId(string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId)) return string.Empty;
        
        // Wyciągamy tylko cyfry z NIP
        var sb = new System.Text.StringBuilder();
        foreach (var c in taxId)
        {
            if (char.IsDigit(c))
                sb.Append(c);
        }
        return sb.ToString();
    }
}
