using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceSystem.Application.Common.Interfaces;

public interface IFileHashService
{
    Task<string> CalculateHashAsync(Stream stream, CancellationToken ct);
}
