using MediatR;
using Microsoft.Extensions.Logging;
using InvoiceSystem.Web.Shared.Interfaces;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public sealed class ImportInvoiceHandler(
    IFileStorageService storageService,
    IFileHashService hashService,
    IDocumentOcrService ocrService,
    ILogger<ImportInvoiceHandler> logger)
    : IRequestHandler<ImportInvoiceCommand, ImportInvoiceResponse>
{
    public async Task<ImportInvoiceResponse> Handle(ImportInvoiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = request.File.OpenReadStream();

            // 1. Wyliczanie Hashu (Detekcja duplikatów w przyszłości przez DB)
            var fileHash = await hashService.CalculateHashAsync(stream, cancellationToken);
            logger.LogInformation("Przetwarzanie pliku o hashu: {Hash}", fileHash);

            // 2. Zapis fizyczny w strukturze datowej
            var extension = Path.GetExtension(request.File.FileName).ToLower();
            var secureFileName = $"{Guid.NewGuid()}{extension}";

            stream.Position = 0; // Reset strumienia po hashowaniu
            var relativePath = await storageService.SaveFileAsync(stream, secureFileName, cancellationToken);

            // 3. Ekstrakcja tekstu (OCR dla obrazów, parser dla PDF)
            var ocrResult = await ocrService.ExtractTextAsync(relativePath, cancellationToken);

            if (!ocrResult.Success)
            {
                logger.LogWarning("OCR zakończony niepowodzeniem dla pliku {Path}: {Error}", relativePath, ocrResult.ErrorMessage);
                return new ImportInvoiceResponse(
                    true,
                    $"Plik zapisany, ale ekstrakcja tekstu nie powiodła się: {ocrResult.ErrorMessage}",
                    relativePath);
            }

            logger.LogInformation("OCR zakończony sukcesem. Typ dokumentu: {Type}, długość tekstu: {Length} znaków",
                ocrResult.DocumentType, ocrResult.ExtractedText.Length);

            return new ImportInvoiceResponse(
                true,
                "Dokument został pomyślnie przetworzony. Tekst wyodrębniony.",
                relativePath,
                ocrResult.ExtractedText,
                ocrResult.DocumentType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd krytyczny podczas importu dokumentu.");
            return new ImportInvoiceResponse(false, "Wystąpił błąd systemowy podczas przetwarzania dokumentu.");
        }
    }
}
