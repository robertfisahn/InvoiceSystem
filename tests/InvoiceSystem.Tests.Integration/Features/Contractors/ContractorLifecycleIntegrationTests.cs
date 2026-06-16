using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Contractors
{
    public class ContractorLifecycleIntegrationTests : IntegrationTestBase
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
        public async Task GetContractorList_ShouldReturnOk_AndDisplaySeededContractors()
        {
            // Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var contractor1 = new Contractor { Name = "Alpha Contractors Sp. z o.o.", TaxId = "111-222-33-44", Address = "Alpha Rd 1" };
                var contractor2 = new Contractor { Name = "Beta Solutions", TaxId = "555-666-77-88", Address = "Beta Ave 2" };
                
                db.Contractors.AddRange(contractor1, contractor2);
                await db.SaveChangesAsync();
            }

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/contractors");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var decodedContent = System.Net.WebUtility.HtmlDecode(content);

            decodedContent.Should().Contain("Alpha Contractors Sp. z o.o.");
            decodedContent.Should().Contain("111-222-33-44");
            decodedContent.Should().Contain("Beta Solutions");
            decodedContent.Should().Contain("555-666-77-88");
        }

        [Fact]
        public async Task GetCreateContractor_ShouldReturnOk_WithForm()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/contractors/create");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("<form");
            content.Should().Contain("name=\"Name\"");
            content.Should().Contain("name=\"TaxId\"");
        }

        [Fact]
        public async Task CreateContractor_ShouldRedirectToContractorList_AndPersistToDb_WhenDataIsValid()
        {
            // Arrange
            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            // Get antiforgery token
            var createPageResponse = await client.GetAsync("/contractors/create");
            var createPageHtml = await createPageResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(createPageHtml);

            var companyName = $"Super Company_{Guid.NewGuid():N}";
            var taxId = "777-888-99-00";
            var address = "Warszawska 45, Warszawa";

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", companyName),
                new KeyValuePair<string, string>("TaxId", taxId),
                new KeyValuePair<string, string>("Address", address),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/contractors/create", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/contractors");

            // Verify in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var saved = await db.Contractors.FirstOrDefaultAsync(c => c.Name == companyName);
                saved.Should().NotBeNull();
                saved!.TaxId.Should().Be("7778889900"); // normalized NIP
                saved.Address.Should().Be(address);
            }
        }

        [Fact]
        public async Task CreateContractor_ShouldReturnFormWithValidationErrors_WhenNameIsEmpty()
        {
            // Arrange
            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var createPageResponse = await client.GetAsync("/contractors/create");
            var createPageHtml = await createPageResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(createPageHtml);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Name", ""),
                new KeyValuePair<string, string>("TaxId", "123-456-78-90"),
                new KeyValuePair<string, string>("Address", "Some Address"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/contractors/create", formContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK); // renders view again
            var content = await response.Content.ReadAsStringAsync();
            var decodedContent = System.Net.WebUtility.HtmlDecode(content);
            decodedContent.Should().Contain("Nazwa kontrahenta jest wymagana.");
        }
    }
}
