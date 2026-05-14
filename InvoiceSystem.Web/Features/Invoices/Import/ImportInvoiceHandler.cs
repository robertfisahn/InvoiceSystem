using MediatR;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public class ImportInvoiceHandler(
    IFileStorageService storageService,
    ILogger<ImportInvoiceHandler> logger) 
    : IRequestHandler<ImportInvoiceCommand, ImportInvoiceResponse>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    public async Task<ImportInvoiceResponse> Handle(ImportInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Walidacja
        if (request.File == null || request.File.Length == 0)
        {
            logger.LogWarning("Próba uploadu pustego pliku.");
            return new ImportInvoiceResponse(false, "Błąd: Nie wybrano pliku.");
        }

        if (request.File.Length > MaxFileSize)
        {
            logger.LogWarning("Plik przekracza limit rozmiaru: {Size} bytes", request.File.Length);
            return new ImportInvoiceResponse(false, "Błąd: Plik jest zbyt duży (maksymalnie 10MB).");
        }

        var extension = Path.GetExtension(request.File.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
        {
            logger.LogWarning("Nieobsługiwany format pliku: {Extension}", extension);
            return new ImportInvoiceResponse(false, "Błąd: Nieobsługiwany format pliku. Dozwolone: PDF, JPG, PNG.");
        }

        // 2. Proces zapisu przez dedykowany serwis
        try 
        {
            var fileName = $"{Guid.NewGuid()}{extension}";
            
            using var stream = request.File.OpenReadStream();
            var savedFileName = await storageService.SaveFileAsync(stream, fileName, cancellationToken);

            return new ImportInvoiceResponse(true, "Plik został pomyślnie zabezpieczony.", savedFileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Wystąpił krytyczny błąd podczas importu pliku.");
            return new ImportInvoiceResponse(false, "Wystąpił błąd systemowy podczas zapisu pliku.");
        }
    }
}
