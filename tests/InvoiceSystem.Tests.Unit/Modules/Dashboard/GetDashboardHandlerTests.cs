using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Auth.Domain;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Dashboard.Features.Dashboard;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Dashboard
{
    public class GetDashboardHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public GetDashboardHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_Dashboard_When_No_Invoices_Exist()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new GetDashboardHandler(db);
            var query = new GetDashboardQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalAmount.Should().Be(0);
            result.TotalCount.Should().Be(0);
            result.PaidAmount.Should().Be(0);
            result.PaidCount.Should().Be(0);
            result.ConfirmedAmount.Should().Be(0);
            result.ConfirmedCount.Should().Be(0);
            result.DraftAmount.Should().Be(0);
            result.DraftCount.Should().Be(0);
            result.PaidRatio.Should().Be(0.0);
            result.RecentInvoices.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_Calculate_Stats_And_Ratios_Correctly()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Client A", TaxId = "111", Address = "Addr" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            // 1. Paid Invoice
            var paidInvoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Paid,
                Date = DateTime.Today.AddDays(-5),
                ContractorId = contractor.Id,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Service 1", Quantity = 2, UnitPrice = 100m, TotalPrice = 200m }
                }
            };
            db.Invoices.Add(paidInvoice);

            // 2. Confirmed Invoice
            var confirmedInvoice = new Invoice
            {
                InvoiceNumber = "FV/2026/002",
                Status = InvoiceStatus.Confirmed,
                Date = DateTime.Today.AddDays(-3),
                ContractorId = contractor.Id,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Service 2", Quantity = 1, UnitPrice = 300m, TotalPrice = 300m }
                }
            };
            db.Invoices.Add(confirmedInvoice);

            // 3. Draft Invoice
            var draftInvoice = new Invoice
            {
                InvoiceNumber = "FV/2026/003",
                Status = InvoiceStatus.Draft,
                Date = DateTime.Today.AddDays(-1),
                ContractorId = contractor.Id,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Service 3", Quantity = 1, UnitPrice = 500m, TotalPrice = 500m }
                }
            };
            db.Invoices.Add(draftInvoice);

            await db.SaveChangesAsync();

            var handler = new GetDashboardHandler(db);
            var query = new GetDashboardQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalAmount.Should().Be(1000m); // 200 + 300 + 500
            result.TotalCount.Should().Be(3);
            
            result.PaidAmount.Should().Be(200m);
            result.PaidCount.Should().Be(1);

            result.ConfirmedAmount.Should().Be(300m);
            result.ConfirmedCount.Should().Be(1);

            result.DraftAmount.Should().Be(500m);
            result.DraftCount.Should().Be(1);

            // Paid Ratio: 200 / 1000 = 20.0%
            result.PaidRatio.Should().Be(20.0);
        }

        [Fact]
        public async Task Handle_Should_Return_At_Most_Five_Recent_Invoices_Ordered_By_Date_And_Id()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Client A", TaxId = "111", Address = "Addr" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            // Create 7 invoices with sequential dates
            var baseDate = DateTime.Today;
            for (int i = 1; i <= 7; i++)
            {
                var invoice = new Invoice
                {
                    InvoiceNumber = $"FV/2026/00{i}",
                    Status = InvoiceStatus.Draft,
                    Date = baseDate.AddDays(i),
                    ContractorId = contractor.Id,
                    Items = new List<InvoiceItem>
                    {
                        new InvoiceItem { Name = $"Item {i}", Quantity = 1, UnitPrice = 10m * i, TotalPrice = 10m * i }
                    }
                };
                db.Invoices.Add(invoice);
            }
            await db.SaveChangesAsync();

            var handler = new GetDashboardHandler(db);
            var query = new GetDashboardQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.RecentInvoices.Should().HaveCount(5);

            // Order should be descending by date.
            // Expected indices of invoices in order: 7, 6, 5, 4, 3
            result.RecentInvoices[0].InvoiceNumber.Should().Be("FV/2026/007");
            result.RecentInvoices[1].InvoiceNumber.Should().Be("FV/2026/006");
            result.RecentInvoices[2].InvoiceNumber.Should().Be("FV/2026/005");
            result.RecentInvoices[3].InvoiceNumber.Should().Be("FV/2026/004");
            result.RecentInvoices[4].InvoiceNumber.Should().Be("FV/2026/003");
        }
    }
}
