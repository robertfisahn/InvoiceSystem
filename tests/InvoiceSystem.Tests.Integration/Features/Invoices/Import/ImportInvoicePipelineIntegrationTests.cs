using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Invoices.Import;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Services.Llm;
using InvoiceSystem.Web.Infrastructure.Services.Ocr;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Invoices.Import
{
    public class ImportInvoicePipelineIntegrationTests : IntegrationTestBase
    {
        private HttpClient CreateAuthenticatedClient(bool allowAutoRedirect = false)
        {
            var options = new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect
            };
            return _factory.CreateClient(options);
        }

        private static string ExtractAntiforgeryToken(string html)
        {
            var match = Regex.Match(html, @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
            if (!match.Success)
            {
                throw new Exception("Antiforgery token not found in the HTML content.");
            }
            return match.Groups[1].Value;
        }

        [Fact]
        public async Task GetImportInvoice_ShouldReturnOk_WithUploadForm()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/invoices/import");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Przeciągnij plik tutaj lub kliknij, aby wgrać z dysku");
        }

        [Fact]
        public async Task UploadFile_ShouldProcessOcrSuccessfully_AndReturnExtractedText()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            var getResponse = await client.GetAsync("/invoices/import");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            // Mock OCR Service return
            var ocrMock = _factory.Services.GetRequiredService<IDocumentOcrService>();
            ocrMock.ClearReceivedCalls();
            ocrMock.ExtractTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult(new OcrResult("Faktura VAT 123/2026. Sprzedawca: ABC, NIP Kupujacego: 9998887766. Razem: 1500 PLN.", "PDF", true)));

            // Prepare multipart form upload
            using var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(fileContent, "file", "invoice_999.pdf");
            multipartContent.Add(new StringContent(token), "__RequestVerificationToken");

            // Act
            var response = await client.PostAsync("/invoices/import", multipartContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var decodedContent = System.Net.WebUtility.HtmlDecode(content);

            decodedContent.Should().Contain("Dokument został pomyślnie przetworzony. Tekst wyodrębniony.");
            decodedContent.Should().Contain("NIP Kupujacego: 9998887766");
        }

        [Fact]
        public async Task AnalyzeOcr_ShouldDetectExistingContractor_WhenTaxIdExistsInDb()
        {
            // Arrange
            var taxId = "9998887766";
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Seed existing contractor
                var contractor = new Contractor
                {
                    Name = "Known Buyer Sp. z o.o.",
                    TaxId = taxId,
                    Address = "Some Address"
                };
                db.Contractors.Add(contractor);
                await db.SaveChangesAsync();
            }

            var client = CreateAuthenticatedClient();

            var getResponse = await client.GetAsync("/invoices/import");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            // Mock LLM Service return
            var llmMock = _factory.Services.GetRequiredService<ILlmService>();
            llmMock.ClearReceivedCalls();
            llmMock.ExtractInvoiceDataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns(Task.FromResult<LlmInvoiceDto?>(new LlmInvoiceDto(
                       BuyerName: "Known Buyer Sp. z o.o.",
                       BuyerTaxId: taxId,
                       BuyerAddress: "Some Address",
                       InvoiceNumber: "F/2026/06",
                       Date: new DateTime(2026, 6, 16),
                       Items: new List<LlmInvoiceItemDto>
                       {
                           new LlmInvoiceItemDto("Consulting Services", 1, 1000m)
                       }
                   )));

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("extractedText", "Sample OCR Text"),
                new KeyValuePair<string, string>("provider", "gemini"),
                new KeyValuePair<string, string>("filePath", "uploads/test.pdf"),
                new KeyValuePair<string, string>("documentType", "PDF"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/import/analyze", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var decodedContent = System.Net.WebUtility.HtmlDecode(content);

            decodedContent.Should().Contain("Analiza AI (gemini) zakończona pomyślnie.");
            decodedContent.Should().Contain("Known Buyer Sp. z o.o.");
            decodedContent.Should().Contain(taxId);
            // Verify UI badge indicating contractor exists
            decodedContent.Should().Contain("Ten kontrahent istnieje już w systemie");
        }

        [Fact]
        public async Task ConfirmOcr_ShouldRedirectToCreateInvoice_WhenContractorExists()
        {
            // Arrange
            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var getResponse = await client.GetAsync("/invoices/import");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            // Seed cache to simulate completed analysis session
            var sessionId = Guid.NewGuid().ToString();
            var sessionData = new OcrSessionData
            {
                FilePath = "uploads/test.pdf",
                BuyerName = "Existing Contractor",
                BuyerTaxId = "1234567890",
                BuyerAddress = "Adres 1",
                InvoiceNumber = "INV/2026/001",
                Date = DateTime.Today,
                Items = new List<OcrSessionItem>
                {
                    new OcrSessionItem { Name = "Item A", Quantity = 2, UnitPrice = 100m }
                }
            };

            var cache = _factory.Services.GetRequiredService<IMemoryCache>();
            cache.Set($"ocr-session-{sessionId}", sessionData, TimeSpan.FromMinutes(5));

            // Seed Contractor so result.ContractorExists becomes true
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Contractors.Add(new Contractor { Name = "Existing Contractor", TaxId = "1234567890", Address = "Adres 1" });
                await db.SaveChangesAsync();
            }

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("SessionId", sessionId),
                new KeyValuePair<string, string>("BuyerName", "Existing Contractor"),
                new KeyValuePair<string, string>("BuyerTaxId", "1234567890"),
                new KeyValuePair<string, string>("BuyerAddress", "Adres 1"),
                new KeyValuePair<string, string>("Date", DateTime.Today.ToString("yyyy-MM-dd")),
                new KeyValuePair<string, string>("Items[0].Name", "Item A"),
                new KeyValuePair<string, string>("Items[0].Quantity", "2"),
                new KeyValuePair<string, string>("Items[0].UnitPrice", "100"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/import/confirm", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/CreateInvoice");
        }

        [Fact]
        public async Task ConfirmOcr_ShouldRedirectToCreateContractor_WhenContractorDoesNotExist()
        {
            // Arrange
            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var getResponse = await client.GetAsync("/invoices/import");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var sessionId = Guid.NewGuid().ToString();
            var sessionData = new OcrSessionData
            {
                FilePath = "uploads/test.pdf",
                BuyerName = "New Contractor",
                BuyerTaxId = "9876543210",
                BuyerAddress = "Adres 2",
                InvoiceNumber = "INV/2026/999",
                Date = DateTime.Today,
                Items = new List<OcrSessionItem>()
            };

            var cache = _factory.Services.GetRequiredService<IMemoryCache>();
            cache.Set($"ocr-session-{sessionId}", sessionData, TimeSpan.FromMinutes(5));

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("SessionId", sessionId),
                new KeyValuePair<string, string>("BuyerName", "New Contractor"),
                new KeyValuePair<string, string>("BuyerTaxId", "9876543210"),
                new KeyValuePair<string, string>("BuyerAddress", "Adres 2"),
                new KeyValuePair<string, string>("Date", DateTime.Today.ToString("yyyy-MM-dd")),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/import/confirm", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Contain("/CreateContractor");
            response.Headers.Location?.ToString().Should().Contain($"sessionId={sessionId}");
        }
    }
}
