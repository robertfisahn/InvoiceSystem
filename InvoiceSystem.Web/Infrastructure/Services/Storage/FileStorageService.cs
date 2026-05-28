using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiceSystem.Web.Infrastructure.Services.Storage;

public sealed class FileStorageService(
    IOptions<StorageSettings> settings, 
    ILogger<FileStorageService> logger) : IFileStorageService
{
    public async Task<string> SaveFileAsync(Stream content, string fileName, CancellationToken ct)
    {
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd").Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullRootPath = Path.Combine(settings.Value.RootPath, datePath);

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
        
        return Path.Combine(datePath, fileName);
    }
}
