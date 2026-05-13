using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public class OllamaService(HttpClient httpClient)
{
    private const string OllamaUrl = "http://localhost:11434/api/generate";

    public async Task<string?> ExtractInvoiceDataJson(string rawText)
    {
        var prompt = $@"
        Jesteś ekspertem od analizy faktur. Wyciągnij dane z poniższego tekstu faktury i zwróć je WYŁĄCZNIE w formacie JSON. 
        Nie pisz żadnego wstępu ani zakończenia. Tylko surowy JSON.

        Format JSON:
        {{
            ""invoiceNumber"": ""numer faktury"",
            ""date"": ""rrrr-mm-dd"",
            ""contractorName"": ""pełna nazwa sprzedawcy/wystawcy"",
            ""totalNet"": 0.00,
            ""totalGross"": 0.00,
            ""items"": [
                {{ ""name"": ""nazwa produktu"", ""quantity"": 1, ""price"": 0.00 }}
            ]
        }}

        Tekst faktury:
        ---
        {rawText}
        ---";

        var requestBody = new
        {
            model = "llama3", // Możesz zmienić na swój ulubiony model
            prompt = prompt,
            stream = false,
            format = "json"
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(OllamaUrl, content);
            if (!response.IsSuccessStatusCode) return null;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("response").GetString();
        }
        catch
        {
            return null;
        }
    }
}
 public class ExtractedInvoiceData
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string ContractorName { get; set; } = string.Empty;
        public decimal TotalNet { get; set; }
        public decimal TotalGross { get; set; }
        public List<ExtractedItem> Items { get; set; } = new();
    }

    public class ExtractedItem
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
