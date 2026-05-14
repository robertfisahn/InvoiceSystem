using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public interface IFileValidationService
{
    (bool IsValid, string Message) ValidateInvoiceUpload(IFormFile file);
    Task<string> CalculateHashAsync(Stream stream, CancellationToken ct);
}

public class FileValidationService : IFileValidationService
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    public (bool IsValid, string Message) ValidateInvoiceUpload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "Nie wybrano pliku.");

        if (file.Length > MaxFileSize)
            return (false, "Plik jest zbyt duży (maksymalnie 10MB).");

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
            return (false, "Nieobsługiwany format pliku. Dozwolone: PDF, JPG, PNG.");

        return (true, string.Empty);
    }

    public async Task<string> CalculateHashAsync(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
