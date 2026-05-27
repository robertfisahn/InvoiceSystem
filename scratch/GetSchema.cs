using System;
using System.IO;
using System.Text.Json;

class Program
{
    static void Main()
    {
        var text = File.ReadAllText(@"C:\Users\rober\Downloads\openapi.json");
        using var doc = JsonDocument.Parse(text);
        var challengeEndpoint = doc.RootElement.GetProperty("paths").GetProperty("/auth/challenge").GetProperty("post");
        Console.WriteLine(challengeEndpoint.GetProperty("requestBody").GetProperty("content").GetProperty("application/json").GetProperty("schema").ToString());
    }
}
