using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Invoices.GenerateSample
{
    public class GenerateSampleIntegrationTests : IntegrationTestBase
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
        public async Task GetGeneratorPage_ShouldReturnOk_WithPageStructure()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/invoices/generator");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Generator Faktur Testowych");
            content.Should().Contain("<textarea id=\"jsonData\"");
        }

        [Fact]
        public async Task GetRandomize_ShouldReturnOk_WithFakeInvoiceJson()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/invoices/generator/randomize");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            root.TryGetProperty("invoiceNumber", out _).Should().BeTrue();
            root.TryGetProperty("seller", out _).Should().BeTrue();
            root.TryGetProperty("buyer", out _).Should().BeTrue();
            root.TryGetProperty("items", out _).Should().BeTrue();
        }

        [Theory]
        [InlineData("pdf", "application/pdf")]
        [InlineData("jpg", "image/jpeg")]
        [InlineData("png", "image/png")]
        public async Task DownloadSampleInvoice_ShouldReturnFile_WhenFormatIsValid(string format, string expectedContentType)
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            // Fetch a random JSON from endpoint
            var randomizeResponse = await client.GetAsync("/invoices/generator/randomize");
            var randomJson = await randomizeResponse.Content.ReadAsStringAsync();

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("jsonData", randomJson),
                new KeyValuePair<string, string>("format", format),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/generator/download", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.ToString().Should().Be(expectedContentType);
            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            fileBytes.Should().NotBeEmpty();
        }

        [Fact]
        public async Task DownloadSampleInvoice_ShouldReturnBadRequest_WhenJsonDataIsMissing()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("jsonData", ""),
                new KeyValuePair<string, string>("format", "pdf"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/generator/download", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Brak danych JSON do wygenerowania pliku.");
        }

        [Fact]
        public async Task DownloadSampleInvoice_ShouldReturnBadRequest_WhenFormatIsInvalid()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("jsonData", "{}"),
                new KeyValuePair<string, string>("format", "invalid_format"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/generator/download", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Nieprawidłowy format. Dozwolone: pdf, jpg, png.");
        }
    }
}
