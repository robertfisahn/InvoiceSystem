using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceSystem.Web.Shared.Interfaces;

public interface IFileHashService
{
    Task<string> CalculateHashAsync(Stream stream, CancellationToken ct);
}
