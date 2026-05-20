using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Shared.Interfaces;
using InvoiceSystem.Web.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tesseract;
using UglyToad.PdfPig;

namespace InvoiceSystem.Web.Infrastructure.Services;

public sealed class DocumentOcrService(
    IOptions<StorageSettings> storageSettings,
    ILogger<DocumentOcrService> logger) : IDocumentOcrService
{
    private readonly StorageSettings _storageSettings = storageSettings.Value;

    public async Task<OcrResult> ExtractTextAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(_storageSettings.RootPath, relativePath);

        if (!File.Exists(fullPath))
        {
            logger.LogError("Plik nie istnieje w lokalizacji: {FullPath}", fullPath);
            return new OcrResult(string.Empty, "Unknown", false, "Plik nie istnieje na serwerze.");
        }

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();

        try
        {
            if (extension == ".pdf")
            {
                logger.LogInformation("Rozpoczynanie parsowania pliku PDF: {Path}", relativePath);
                var text = ExtractTextFromPdf(fullPath);
                return new OcrResult(text, "PDF", true);
            }
            else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                logger.LogInformation("Rozpoczynanie OCR dla obrazu: {Path}", relativePath);
                
                // Zapewniamy, że pliki językowe pol+eng istnieją w tessdata
                await EnsureTrainedDataExistsAsync(cancellationToken);
                
                var text = ExtractTextFromImage(fullPath);
                return new OcrResult(text, "Image", true);
            }
            else
            {
                logger.LogWarning("Niewspierany format pliku: {Extension}", extension);
                return new OcrResult(string.Empty, extension, false, $"Niewspierany format pliku: {extension}. Dozwolone pliki to PDF, JPG, PNG.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Błąd podczas ekstrakcji tekstu z pliku: {Path}", relativePath);
            return new OcrResult(string.Empty, extension, false, $"Błąd parsowania: {ex.Message}");
        }
    }

    private string ExtractTextFromPdf(string pdfPath)
    {
        var textBuilder = new StringBuilder();
        using (var pdf = PdfDocument.Open(pdfPath))
        {
            foreach (var page in pdf.GetPages())
            {
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    textBuilder.AppendLine(pageText);
                }
            }
        }
        return textBuilder.ToString();
    }

    private string ExtractTextFromImage(string imagePath)
    {
        // Tesseract .NET 5.2.0 szuka plików .traineddata bezpośrednio w podanym dataPath.
        var dataPath = Path.Combine(_storageSettings.RootPath, "tessdata");

        using var engine = new TesseractEngine(dataPath, "pol+eng", EngineMode.Default);
        using var img = Pix.LoadFromFile(imagePath);
        using var page = engine.Process(img);
        
        return page.GetText();
    }

    private async Task EnsureTrainedDataExistsAsync(CancellationToken cancellationToken)
    {
        var tessdataPath = Path.Combine(_storageSettings.RootPath, "tessdata");
        if (!Directory.Exists(tessdataPath))
        {
            Directory.CreateDirectory(tessdataPath);
        }

        var languages = new[] { "pol", "eng" };
        using var client = new HttpClient();

        foreach (var lang in languages)
        {
            var filePath = Path.Combine(tessdataPath, $"{lang}.traineddata");
            if (!File.Exists(filePath))
            {
                logger.LogInformation("Pobieranie pliku językowego {Lang} dla Tesseract OCR...", lang);
                var url = $"https://github.com/tesseract-ocr/tessdata/raw/main/{lang}.traineddata";
                try
                {
                    var responseBytes = await client.GetByteArrayAsync(url, cancellationToken);
                    await File.WriteAllBytesAsync(filePath, responseBytes, cancellationToken);
                    logger.LogInformation("Pobrano i zapisano plik {Lang}.traineddata", lang);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Błąd podczas automatycznego pobierania pliku językowego {Lang} dla OCR.", lang);
                    throw new InvalidOperationException($"Silnik OCR wymaga pliku językowego {lang}.traineddata, a jego automatyczne pobieranie nie powiodło się. Pobierz plik ręcznie i umieść go w {tessdataPath}.", ex);
                }
            }
        }
    }
}
