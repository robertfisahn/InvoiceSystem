using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public class StorageSettings
{
    public string RootPath { get; set; } = "App_Data/Storage/Incoming";
}

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream content, string fileName, CancellationToken ct);
}

public class FileStorageService(
    IOptions<StorageSettings> settings, 
    IWebHostEnvironment environment,
    ILogger<FileStorageService> logger) : IFileStorageService
{
    public async Task<string> SaveFileAsync(Stream content, string fileName, CancellationToken ct)
    {
        // Budujemy pełną ścieżkę na podstawie ContentRootPath (Private Storage)
        var fullRootPath = Path.Combine(environment.ContentRootPath, settings.Value.RootPath);

        if (!Directory.Exists(fullRootPath))
        {
            logger.LogInformation("Tworzenie brakującego katalogu storage: {Path}", fullRootPath);
            Directory.CreateDirectory(fullRootPath);
        }

        var fullPath = Path.Combine(fullRootPath, fileName);
        
        logger.LogDebug("Zapisywanie pliku: {FileName}", fileName);

        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        logger.LogInformation("Plik zapisany pomyślnie: {FileName}", fileName);
        
        return fileName;
    }
}
