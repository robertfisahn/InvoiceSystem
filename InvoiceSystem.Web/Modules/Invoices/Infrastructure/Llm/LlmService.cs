using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiceSystem.Web.Modules.Invoices.Infrastructure.Llm;

public sealed class LlmService(
    IHttpClientFactory httpClientFactory,
    IOptions<AiSettings> aiSettings,
    ILogger<LlmService> logger) : ILlmService
{
    private readonly AiSettings _settings = aiSettings.Value;

    public async Task<LlmInvoiceDto?> ExtractInvoiceDataAsync(string ocrText, string provider, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Rozpoczynanie ekstrakcji danych przy użyciu dostawcy AI: {Provider}", provider);

            string jsonResponse = string.Empty;
            if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                jsonResponse = await CallOllamaAsync(ocrText, cancellationToken);
            }
            else if (provider.Equals("Groq", StringComparison.OrdinalIgnoreCase))
            {
                jsonResponse = await CallGroqAsync(ocrText, cancellationToken);
            }
            else
            {
                logger.LogError("Niewspierany dostawca AI: {Provider}", provider);
                return null;
            }

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                logger.LogWarning("AI zwróciło pustą odpowiedź.");
                return null;
            }

            // Oczyszczanie odpowiedzi z ewentualnych znaczników markdownu ```json ... ```
            var cleanJson = CleanJsonString(jsonResponse);
            logger.LogInformation("Oczyszczony JSON z LLM: {Json}", cleanJson);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var extractedData = JsonSerializer.Deserialize<LlmInvoiceDto>(cleanJson, options);
            return extractedData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Wystąpił błąd podczas komunikacji lub parsowania z LLM.");
            return null;
        }
    }

    private async Task<string> CallOllamaAsync(string ocrText, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_settings.OllamaUrl.TrimEnd('/') + "/");

        var systemPrompt = GetSystemPrompt();

        var requestBody = new
        {
            model = _settings.OllamaModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = ocrText }
            },
            stream = false,
            format = "json" // Ollama wymusi strukturę JSON w wyjściu
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        logger.LogInformation("Wysyłanie zapytania do lokalnej Ollamy ({Model}) na url: {Url}", _settings.OllamaModel, _settings.OllamaUrl);
        var response = await client.PostAsync("api/chat", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Ollama zwróciła błąd HTTP {Code}: {Response}", response.StatusCode, errContent);
            return string.Empty;
        }

        var resContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(resContent);
        if (doc.RootElement.TryGetProperty("message", out var messageProp) && 
            messageProp.TryGetProperty("content", out var contentProp))
        {
            return contentProp.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private async Task<string> CallGroqAsync(string ocrText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.GroqApiKey))
        {
            logger.LogError("Brak zdefiniowanego klucza API do Groqa w konfiguracji (GroqApiKey jest puste).");
            throw new InvalidOperationException("Integracja z Groq wymaga podania klucza API w pliku konfiguracyjnym lub zmiennej środowiskowej AiSettings__GroqApiKey.");
        }

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.GroqApiKey);

        var systemPrompt = GetSystemPrompt();

        var requestBody = new
        {
            model = _settings.GroqModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = ocrText }
            },
            response_format = new { type = "json_object" } // Groq API wymusi JSON format
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        logger.LogInformation("Wysyłanie zapytania do Groq API ({Model})", _settings.GroqModel);
        var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Groq API zwróciło błąd HTTP {Code}: {Response}", response.StatusCode, errContent);
            return string.Empty;
        }

        var resContent = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(resContent);
        if (doc.RootElement.TryGetProperty("choices", out var choicesProp) && 
            choicesProp.GetArrayLength() > 0)
        {
            var firstChoice = choicesProp[0];
            if (firstChoice.TryGetProperty("message", out var messageProp) && 
                messageProp.TryGetProperty("content", out var contentProp))
            {
                return contentProp.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private string GetSystemPrompt()
    {
        return "You are an expert data extraction assistant. Your task is to analyze raw, messy OCR text from an invoice and extract structured fields.\n" +
               "CRITICAL RULES:\n" +
               "1. The entity named 'InvoiceSystem Enterprise' (Tax ID/NIP: 1234567890, address: ul. Technologiczna 12) is ALWAYS the SELLER. Do NOT extract it as the Buyer.\n" +
               "2. Extract the BUYER (the customer/client purchasing the services/products, e.g., 'Rak, Lewandowski and Bednarski'). Look for the OTHER business entity in the text.\n" +
               "3. OCR text often reads two-column layouts row-by-row (e.g. 'SellerName BuyerName', 'SellerAddress BuyerAddress', 'SellerNip BuyerNip'). Split these lines carefully to extract the buyer's name, address, and NIP (e.g. NIP: 572-299-69-03) rather than the seller's.\n" +
               "4. For BuyerTaxId, output ONLY clean digits (e.g. '5722996903').\n" +
               "5. For Date, format it strictly as YYYY-MM-DD. If the document states 5/21/2026, return '2026-05-21'.\n\n" +
               "You must return ONLY a JSON object matching this schema exactly, and nothing else. Do not output any markdown blocks, do not wrap the JSON in ```json or ```, do not include preamble, notes, explanations, or extra text.\n" +
               "Strict JSON Schema:\n" +
               "{\n" +
               "  \"BuyerName\": \"string (name of the client company/buyer)\",\n" +
               "  \"BuyerTaxId\": \"string (NIP of the buyer, digits only)\",\n" +
               "  \"BuyerAddress\": \"string (complete postal address of the buyer)\",\n" +
               "  \"InvoiceNumber\": \"string (the invoice number/id from the document)\",\n" +
               "  \"Date\": \"string (YYYY-MM-DD)\",\n" +
               "  \"Items\": [\n" +
               "    {\n" +
               "      \"Name\": \"string (description of the item/service)\",\n" +
               "      \"Quantity\": number,\n" +
               "      \"UnitPrice\": number\n" +
               "    }\n" +
               "  ]\n" +
               "}";
    }

    private string CleanJsonString(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var clean = text.Trim();
        if (clean.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean.Substring(7);
        }
        else if (clean.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean.Substring(3);
        }

        if (clean.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean.Substring(0, clean.Length - 3);
        }

        return clean.Trim();
    }
}
