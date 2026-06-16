using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
    public class InvoiceQueryEndpointsIntegrationTests : IntegrationTestBase
    {
        private HttpClient CreateAuthenticatedClient(bool allowAutoRedirect = false)
        {
            var options = new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect
            };
            return _factory.CreateClient(options);
        }

        private async Task<(Contractor, Invoice)> PrepareTestInvoiceAsync(AppDbContext db, InvoiceStatus initialStatus)
        {
            var contractor = new Contractor
            {
                Name = "Query Endpoint Customer Sp. z o.o.",
                TaxId = "5250000789",
                Address = "Jasna 10, Warszawa"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                ContractorId = contractor.Id,
                InvoiceNumber = $"INV/QUERY/{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}",
                Date = DateTime.UtcNow,
                Status = initialStatus,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem
                    {
                        Name = "Query Test Item",
                        Quantity = 1,
                        UnitPrice = 100m,
                        TotalPrice = 100m
                    }
                }
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            return (contractor, invoice);
        }

        [Fact]
        public async Task GetInvoiceList_ShouldReturnOk_WithInvoicesPageStructure()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/invoices");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Faktury");
            content.Should().Contain("invoicesTable");
        }

        [Fact]
        public async Task GetCreateInvoicePage_ShouldReturnOk_WithFormStructure()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/invoices/create");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Nowa Faktura");
            content.Should().Contain("Command.ContractorId");
        }

        [Fact]
        public async Task GetInvoiceDetails_ShouldReturnOk_WhenInvoiceExists()
        {
            // Arrange
            Invoice invoice;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Draft);
            }

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync($"/invoices/{invoice.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain(invoice.InvoiceNumber);
            content.Should().Contain("invoiceWrapper");
            content.Should().Contain("printInvoice()");
        }

        [Fact]
        public async Task GetInvoiceDetails_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/invoices/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetInvoiceForUpdatePage_ShouldReturnOk_WhenInvoiceExists()
        {
            // Arrange
            Invoice invoice;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Draft);
            }

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync($"/invoices/update/{invoice.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Dane podstawowe");
            content.Should().Contain("invoiceForm");
            content.Should().Contain("itemsContainer");
        }

        [Fact]
        public async Task GetInvoiceForUpdatePage_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
        {
            // Arrange
            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/invoices/update/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
