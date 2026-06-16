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
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Invoices
{
    public class InvoiceControllerErrorHandlingIntegrationTests : IntegrationTestBase
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

        private async Task<(Contractor, Invoice)> PrepareTestInvoiceAsync(AppDbContext db, InvoiceStatus initialStatus)
        {
            var contractor = new Contractor
            {
                Name = "Error Handling Customer Sp. z o.o.",
                TaxId = "5250000456",
                Address = "Jasna 5, Warszawa"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                ContractorId = contractor.Id,
                InvoiceNumber = $"INV/TEST/{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}",
                Date = DateTime.UtcNow,
                Status = initialStatus,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem
                    {
                        Name = "Initial Item 1",
                        Quantity = 2,
                        UnitPrice = 50m,
                        TotalPrice = 100m
                    }
                }
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            return (contractor, invoice);
        }

        [Fact]
        public async Task UpdateInvoice_ShouldReturnBadRequest_WhenIdInUrlAndBodyMismatch()
        {
            // Arrange
            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Command.Id", "999"), // mismatch with 123 in URL
                new KeyValuePair<string, string>("Command.ContractorId", "1"),
                new KeyValuePair<string, string>("Command.InvoiceNumber", "FV/123"),
                new KeyValuePair<string, string>("Command.Date", DateTime.Today.ToString("yyyy-MM-dd")),
                new KeyValuePair<string, string>("Command.Items[0].Name", "Valid Item"),
                new KeyValuePair<string, string>("Command.Items[0].Quantity", "1"),
                new KeyValuePair<string, string>("Command.Items[0].UnitPrice", "10"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/update/123", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateInvoice_ShouldRedirectToInvoiceList_WhenInvoiceDoesNotExist()
        {
            // Arrange
            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Command.Id", "99999"), // non-existent ID
                new KeyValuePair<string, string>("Command.ContractorId", "1"),
                new KeyValuePair<string, string>("Command.InvoiceNumber", "FV/99999"),
                new KeyValuePair<string, string>("Command.Date", DateTime.Today.ToString("yyyy-MM-dd")),
                new KeyValuePair<string, string>("Command.Items[0].Name", "Valid Item"),
                new KeyValuePair<string, string>("Command.Items[0].Quantity", "1"),
                new KeyValuePair<string, string>("Command.Items[0].UnitPrice", "10"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/update/99999", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/invoices");
        }

        [Fact]
        public async Task UpdateInvoice_ShouldRedirectToInvoiceList_WhenInvoiceIsNotDraft()
        {
            // Arrange
            Invoice invoice;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Confirmed);
            }

            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Command.Id", invoice.Id.ToString()),
                new KeyValuePair<string, string>("Command.ContractorId", invoice.ContractorId.ToString()),
                new KeyValuePair<string, string>("Command.InvoiceNumber", "FV/ATTEMPT-UPDATE"),
                new KeyValuePair<string, string>("Command.Date", DateTime.Today.ToString("yyyy-MM-dd")),
                new KeyValuePair<string, string>("Command.Items[0].Name", "Valid Item"),
                new KeyValuePair<string, string>("Command.Items[0].Quantity", "1"),
                new KeyValuePair<string, string>("Command.Items[0].UnitPrice", "10"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync($"/invoices/update/{invoice.Id}", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/invoices");
        }

        [Fact]
        public async Task UpdateInvoice_ShouldReturnOkWithValidationErrors_WhenCommandIsInvalid()
        {
            // Arrange
            Invoice invoice;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Draft);
            }

            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Command.Id", invoice.Id.ToString()),
                new KeyValuePair<string, string>("Command.ContractorId", "0"), // invalid: ContractorId must be > 0
                new KeyValuePair<string, string>("Command.InvoiceNumber", invoice.InvoiceNumber),
                new KeyValuePair<string, string>("Command.Date", DateTime.Today.ToString("yyyy-MM-dd")),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync($"/invoices/update/{invoice.Id}", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Wybierz kontrahenta");
        }

        [Fact]
        public async Task DeleteInvoice_ShouldRedirectToInvoiceList_WhenInvoiceDoesNotExist()
        {
            // Arrange
            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync("/invoices/delete/99999", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/invoices");
        }

        [Fact]
        public async Task DeleteInvoice_ShouldRedirectToInvoiceList_WhenInvoiceIsNotDraft()
        {
            // Arrange
            Invoice invoice;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Confirmed);
            }

            var client = CreateAuthenticatedClient(allowAutoRedirect: false);

            var getResponse = await client.GetAsync("/invoices/generator");
            var getHtml = await getResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(getHtml);

            var formFields = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            });

            // Act
            var response = await client.PostAsync($"/invoices/delete/{invoice.Id}", formFields);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location?.ToString().Should().Be("/invoices");
        }
    }
}
