using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceSystem.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream content, string fileName, CancellationToken ct);
}
