using System.Text.Json;
using Bogus;
using MediatR;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GenerateSample;

public sealed class GenerateSampleHandler : IRequestHandler<GenerateSampleCommand, GenerateSampleResponse>
{
    public GenerateSampleHandler()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<GenerateSampleResponse> Handle(GenerateSampleCommand request, CancellationToken cancellationToken)
    {
        var model = JsonSerializer.Deserialize<SampleInvoiceModel>(request.JsonData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Niepoprawny format danych JSON.");

        var document = new SampleInvoiceDocument(model);

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (request.Format)
        {
            case SampleInvoiceFormat.Pdf:
                fileBytes = document.GeneratePdf();
                contentType = "application/pdf";
                fileName = $"Faktura_Testowa_{model.InvoiceNumber}.pdf";
                break;

            case SampleInvoiceFormat.Jpg:
                fileBytes = GenerateImage(document, ImageFormat.Jpeg);
                contentType = "image/jpeg";
                fileName = $"Faktura_Testowa_{model.InvoiceNumber}.jpg";
                break;

            case SampleInvoiceFormat.Png:
                fileBytes = GenerateImage(document, ImageFormat.Png);
                contentType = "image/png";
                fileName = $"Faktura_Testowa_{model.InvoiceNumber}.png";
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.FromResult(new GenerateSampleResponse(fileBytes, contentType, fileName));
    }

    private static byte[] GenerateImage(IDocument document, ImageFormat format)
    {
        var images = document.GenerateImages(new ImageGenerationSettings
        {
            ImageFormat = format,
            ImageCompressionQuality = ImageCompressionQuality.Best,
            RasterDpi = 150
        });

        return images.First();
    }

    /// <summary>
    /// Generuje losowe dane testowe faktury w formie JSON.
    /// Wywoływane bezpośrednio z kontrolera (nie przez MediatR, bo to czysta logika pomocnicza).
    /// </summary>
    public static SampleInvoiceModel GenerateFakeInvoiceData()
    {
        var faker = new Faker("pl");

        var seller = new SampleCompany(
            "InvoiceSystem Enterprise",
            "ul. Technologiczna 12",
            "Gdańsk",
            "80-001",
            "1234567890"
        );

        var buyer = new SampleCompany(
            faker.Company.CompanyName(),
            faker.Address.StreetAddress(),
            faker.Address.City(),
            faker.Address.ZipCode(),
            faker.Random.Replace("###-###-##-##")
        );

        var itServices = new[]
        {
            "Projektowanie UX Enterprise systemu",
            "Analiza Big Data (Kwartalna)",
            "Wsparcie techniczne 24/7 (SLA)",
            "Licencja IDE JetBrains",
            "Hosting WWW - Pakiet Pro",
            "Audyt Bezpieczeństwa (Pentest)",
            "Optymalizacja CI/CD",
            "Poprawki w aplikacji Flutter",
            "Subskrypcja Serwera Cloud (M-1)",
            "Serwis aplikacji webowej",
            "Konfiguracja Firewall"
        };

        var items = new List<SampleInvoiceItem>();
        var itemsCount = faker.Random.Int(1, 4);
        var chosenServices = faker.Random.ListItems(itServices, itemsCount);

        foreach (var service in chosenServices)
        {
            items.Add(new SampleInvoiceItem(
                service,
                Math.Round(faker.Random.Decimal(150, 4500), 2),
                faker.Random.Int(1, 3)
            ));
        }

        var invoiceNumber = $"FV/{DateTime.Now.Year}/{DateTime.Now.Month:D2}/{faker.Random.Int(100, 999)}";

        return new SampleInvoiceModel(
            invoiceNumber,
            faker.Date.Recent(30),
            seller,
            buyer,
            items
        );
    }
}
