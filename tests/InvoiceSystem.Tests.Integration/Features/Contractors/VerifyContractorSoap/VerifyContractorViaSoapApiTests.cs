using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Contractors.VerifyContractorSoap
{
    public class VerifyContractorViaSoapApiTests : IntegrationTestBase
    {
        private HttpClient CreateAuthenticatedClient()
        {
            var client = _factory.CreateClient();
            
            // Set the Host header to match the dynamically allocated port of our running SOAP mock server.
            // This ensures the handler's HttpContextAccessor resolves the correct Mock URL.
            var soapUri = new Uri(_soapMockServerUrl);
            client.DefaultRequestHeaders.Host = soapUri.Authority;
            
            return client;
        }

        [Fact]
        public async Task VerifySoap_ShouldReturnUnauthorized_WhenNoAuthProvided()
        {
            // Arrange
            var client = CreateAuthenticatedClient();
            client.DefaultRequestHeaders.Add("X-Skip-Test-Auth", "true"); // Instructs TestAuthHandler to skip authenticating

            // Act
            var response = await client.PostAsync("/api/contractors/1/verify-soap", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task VerifySoap_ShouldReturnOk_WhenContractorExistsInRegistry()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contractor = new Contractor
            {
                Name = "Pending Verification Co",
                TaxId = "2222222222",
                Address = "Test street 123"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.PostAsync($"/api/contractors/{contractor.Id}/verify-soap", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Firma Testowa NIP 222 (Aktywny VAT)");
            content.Should().Contain("ACTIVE");
        }

        [Fact]
        public async Task VerifySoap_ShouldReturnOk_WithNotFoundStatus_WhenContractorNotFoundInRegistry()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contractor = new Contractor
            {
                Name = "Unknown Company",
                TaxId = "5555555555",
                Address = "Some Address"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.PostAsync($"/api/contractors/{contractor.Id}/verify-soap", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("NOT_FOUND");
        }

        [Fact]
        public async Task VerifySoap_ShouldReturnBadRequest_WhenContractorDoesNotExistInDatabase()
        {
            // Arrange
            var client = CreateAuthenticatedClient();
            var nonExistentContractorId = 999999;

            // Act
            var response = await client.PostAsync($"/api/contractors/{nonExistentContractorId}/verify-soap", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Contractor not found.");
        }
    }
}
