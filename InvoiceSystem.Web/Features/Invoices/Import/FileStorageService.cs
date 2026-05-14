using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace InvoiceSystem.Web.Features.Invoices.Import;

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
        // Struktura datowa: YYYY/MM/DD
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd").Replace("/", Path.DirectorySeparatorChar.ToString());
        var relativePath = Path.Combine(settings.Value.RootPath, datePath);
        var fullRootPath = Path.Combine(environment.ContentRootPath, relativePath);

        if (!Directory.Exists(fullRootPath))
        {
            Directory.CreateDirectory(fullRootPath);
        }

        var fullPath = Path.Combine(fullRootPath, fileName);
        
        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        logger.LogInformation("Plik zapisany w strukturze datowej: {Path}", fullPath);
        
        // Zwracamy relatywną ścieżkę do zapisu w bazie
        return Path.Combine(datePath, fileName);
    }
}
