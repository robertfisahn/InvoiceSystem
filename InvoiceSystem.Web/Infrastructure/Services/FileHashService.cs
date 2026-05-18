using InvoiceSystem.Web.Shared.Interfaces;
using System.Security.Cryptography;

namespace InvoiceSystem.Web.Infrastructure.Services;

public class FileHashService : IFileHashService
{
    public async Task<string> CalculateHashAsync(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
