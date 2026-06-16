using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Dashboard
{
    public class DashboardIntegrationTests : IntegrationTestBase
    {
        private HttpClient CreateAuthenticatedClient()
        {
            return _factory.CreateClient();
        }

        [Fact]
        public async Task GetDashboard_ShouldReturnOk_AndDisplayCorrectStatistics()
        {
            // Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Seed contractors
                var contractor = new Contractor
                {
                    Name = "Dashboard Test Contractor",
                    TaxId = "1234567890",
                    Address = "Test Address"
                };
                db.Contractors.Add(contractor);
                await db.SaveChangesAsync();

                // Seed draft, confirmed, and paid invoices
                var draftInvoice = new Invoice
                {
                    ContractorId = contractor.Id,
                    InvoiceNumber = "INV-DRAFT-123",
                    Date = DateTime.UtcNow.AddDays(-2),
                    Status = InvoiceStatus.Draft,
                    Items = new List<InvoiceItem>
                    {
                        new InvoiceItem { Name = "Item A", Quantity = 1, UnitPrice = 100m, TotalPrice = 100m }
                    }
                };

                var confirmedInvoice = new Invoice
                {
                    ContractorId = contractor.Id,
                    InvoiceNumber = "INV-CONF-456",
                    Date = DateTime.UtcNow.AddDays(-1),
                    Status = InvoiceStatus.Confirmed,
                    Items = new List<InvoiceItem>
                    {
                        new InvoiceItem { Name = "Item B", Quantity = 2, UnitPrice = 150m, TotalPrice = 300m }
                    }
                };

                var paidInvoice = new Invoice
                {
                    ContractorId = contractor.Id,
                    InvoiceNumber = "INV-PAID-789",
                    Date = DateTime.UtcNow,
                    Status = InvoiceStatus.Paid,
                    Items = new List<InvoiceItem>
                    {
                        new InvoiceItem { Name = "Item C", Quantity = 3, UnitPrice = 200m, TotalPrice = 600m }
                    }
                };

                db.Invoices.AddRange(draftInvoice, confirmedInvoice, paidInvoice);
                await db.SaveChangesAsync();
            }

            var client = CreateAuthenticatedClient();

            // Act
            var response = await client.GetAsync("/dashboard");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var decodedContent = System.Net.WebUtility.HtmlDecode(content);

            // Verify totals and status statistics exist on dashboard HTML (handles both PL and EN locale formats)
            bool containsTotal = decodedContent.Contains("1 000,00") || 
                                 decodedContent.Contains("1\u00A0000,00") || 
                                 decodedContent.Contains("1,000.00");
            containsTotal.Should().BeTrue("Should contain total amount: 1000.00");

            bool containsPaid = decodedContent.Contains("600,00") || 
                                decodedContent.Contains("600.00");
            containsPaid.Should().BeTrue("Should contain paid amount: 600.00");

            bool containsConfirmed = decodedContent.Contains("300,00") || 
                                     decodedContent.Contains("300.00");
            containsConfirmed.Should().BeTrue("Should contain confirmed amount: 300.00");

            bool containsDraft = decodedContent.Contains("100,00") || 
                                 decodedContent.Contains("100.00");
            containsDraft.Should().BeTrue("Should contain draft amount: 100.00");

            // Verify paid ratio is 60.0% (600 / 1000 * 100 = 60)
            decodedContent.Should().Contain("60%");

            // Verify recent invoices list contains numbers
            decodedContent.Should().Contain("INV-DRAFT-123");
            decodedContent.Should().Contain("INV-CONF-456");
            decodedContent.Should().Contain("INV-PAID-789");
        }
    }
}
