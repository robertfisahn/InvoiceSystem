using MediatR;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public class ImportInvoiceHandler(IWebHostEnvironment environment) 
    : IRequestHandler<ImportInvoiceCommand, ImportInvoiceResponse>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    public async Task<ImportInvoiceResponse> Handle(ImportInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Walidacja podstawowa
        if (request.File == null || request.File.Length == 0)
        {
            return new ImportInvoiceResponse(false, "Błąd: Nie wybrano pliku.");
        }

        if (request.File.Length > MaxFileSize)
        {
            return new ImportInvoiceResponse(false, "Błąd: Plik jest zbyt duży (maksymalnie 10MB).");
        }

        var extension = Path.GetExtension(request.File.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
        {
            return new ImportInvoiceResponse(false, "Błąd: Nieobsługiwany format pliku. Dozwolone: PDF, JPG, PNG.");
        }

        // 2. Przygotowanie PRIVATE STORAGE (poza wwwroot)
        // Używamy ContentRootPath zamiast WebRootPath
        var storagePath = Path.Combine(environment.ContentRootPath, "App_Data", "Storage", "Incoming");
        
        if (!Directory.Exists(storagePath))
        {
            Directory.CreateDirectory(storagePath);
        }

        // 3. Generowanie bezpiecznej nazwy pliku
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(storagePath, fileName);

        // 4. Zapis fizyczny
        try 
        {
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // W prawdziwym systemie tutaj byłby ILogger
            return new ImportInvoiceResponse(false, $"Błąd podczas zapisu pliku: {ex.Message}");
        }

        return new ImportInvoiceResponse(true, "Plik został pomyślnie przesłany i zabezpieczony.", fileName);
    }
}
