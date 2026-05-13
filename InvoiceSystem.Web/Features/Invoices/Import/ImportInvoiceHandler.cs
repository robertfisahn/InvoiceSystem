using MediatR;
using Microsoft.AspNetCore.Hosting;
using UglyToad.PdfPig;
using System.IO;
using System.Text.Json;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public class ImportInvoiceHandler(IWebHostEnvironment environment, OllamaService ollamaService) 
    : IRequestHandler<ImportInvoiceCommand, ImportInvoiceResponse>
{
    public async Task<ImportInvoiceResponse> Handle(ImportInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return new ImportInvoiceResponse(false, "Nie wybrano pliku.");
        }

        var extension = Path.GetExtension(request.File.FileName).ToLower();
        var uploadsDir = Path.Combine(environment.WebRootPath, "uploads", "incoming");
        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream, cancellationToken);
        }

        // KROK 2: Wyciąganie tekstu (OCR / PDF Text)
        string rawText = "";
        if (extension == ".pdf")
        {
            try 
            {
                using var pdf = PdfDocument.Open(filePath);
                foreach (var page in pdf.GetPages())
                {
                    rawText += page.Text + " ";
                }
            }
            catch (Exception ex)
            {
                return new ImportInvoiceResponse(false, $"Błąd odczytu PDF: {ex.Message}");
            }
        }
        else 
        {
            // Dla obrazków w przyszłości dodamy model wizyjny (Multimodal)
            return new ImportInvoiceResponse(true, "Przesłano obrazek. Analiza obrazków będzie dostępna wkrótce.", $"/uploads/incoming/{fileName}");
        }

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new ImportInvoiceResponse(false, "Nie udało się wyodrębnić tekstu z dokumentu.");
        }

        // KROK 3: Analiza przez AI (Ollama)
        var jsonResult = await ollamaService.ExtractInvoiceDataJson(rawText);
        
        if (string.IsNullOrEmpty(jsonResult))
        {
            return new ImportInvoiceResponse(true, "Plik przesłany, ale AI nie zwróciło danych. Wypełnij formularz ręcznie.", $"/uploads/incoming/{fileName}");
        }

        return new ImportInvoiceResponse(true, "Analiza zakończona sukcesem!", $"/uploads/incoming/{fileName}") 
        { 
            ExtractedData = jsonResult 
        };
    }
}
