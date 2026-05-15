using MediatR;
using Microsoft.Extensions.Logging;
using InvoiceSystem.Application.Common.Interfaces;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public class ImportInvoiceHandler(
    IFileStorageService storageService,
    IFileHashService hashService,
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

            return new ImportInvoiceResponse(true, "Dokument został pomyślnie przetworzony i zabezpieczony.", relativePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd krytyczny podczas importu dokumentu.");
            return new ImportInvoiceResponse(false, "Wystąpił błąd systemowy podczas przetwarzania dokumentu.");
        }
    }
}
