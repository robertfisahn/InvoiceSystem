using System;
using System.IO;
using System.Text.Json;
using System.Linq;

class Program
{
    static void Main()
    {
        var text = File.ReadAllText(@"C:\Users\rober\Downloads\openapi.json");
        using var doc = JsonDocument.Parse(text);
        var paths = doc.RootElement.GetProperty("paths").EnumerateObject();
        foreach (var p in paths)
        {
            if (p.Name.Contains("Challenge", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Found: " + p.Name);
            }
        }
    }
}
