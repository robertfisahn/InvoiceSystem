using InvoiceSystem.Application.Common.Interfaces;
using System.Security.Cryptography;

namespace InvoiceSystem.Infrastructure.Services;

public class FileHashService : IFileHashService
{
    public async Task<string> CalculateHashAsync(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
