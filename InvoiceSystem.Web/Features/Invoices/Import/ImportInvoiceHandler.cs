using MediatR;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public class ImportInvoiceHandler(
    IFileStorageService storageService,
    IFileValidationService validationService,
    ILogger<ImportInvoiceHandler> logger) 
    : IRequestHandler<ImportInvoiceCommand, ImportInvoiceResponse>
{
    public async Task<ImportInvoiceResponse> Handle(ImportInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Walidacja formatu i rozmiaru
        var validation = validationService.ValidateInvoiceUpload(request.File);
        if (!validation.IsValid)
        {
            logger.LogWarning("Nieudana walidacja pliku: {Message}", validation.Message);
            return new ImportInvoiceResponse(false, validation.Message);
        }

        try 
        {
            using var stream = request.File.OpenReadStream();
            
            // 2. Wyliczanie Hashu (Detekcja duplikatów w przyszłości przez DB)
            var fileHash = await validationService.CalculateHashAsync(stream, cancellationToken);
            logger.LogInformation("Przetwarzanie pliku o hashu: {Hash}", fileHash);

            // 3. Zapis fizyczny w strukturze datowej
            var extension = Path.GetExtension(request.File.FileName).ToLower();
            var secureFileName = $"{Guid.NewGuid()}{extension}";
            
            stream.Position = 0; // Reset strumienia po hashowaniu
            var relativePath = await storageService.SaveFileAsync(stream, secureFileName, cancellationToken);

            return new ImportInvoiceResponse(true, "Dokument został pomyślnie przetworzony i zabezpieczony.", relativePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd krytyczny podczas importu dokumentu.");
            return new ImportInvoiceResponse(false, "Wystąpił błąd systemowy podczas przetwarzania dokumentu.");
        }
    }
}
