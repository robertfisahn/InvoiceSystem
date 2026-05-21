namespace InvoiceSystem.Web.Infrastructure.Configuration;

public sealed class AiSettings
{
    public const string SectionName = "AiSettings";

    public string Provider { get; init; } = "Ollama"; // Domyślny dostawca w konfiguracji
    public string OllamaUrl { get; init; } = "http://localhost:11434";
    public string OllamaModel { get; init; } = "llama3";
    public string GroqApiKey { get; init; } = string.Empty;
    public string GroqModel { get; init; } = "llama3-8b-8192";
}
