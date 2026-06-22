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
using InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceDetails;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Invoices.GetInvoiceDetails
{
    public class GetInvoiceDetailsHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public GetInvoiceDetailsHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Null_When_Invoice_Not_Found()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new GetInvoiceDetailsHandler(db);
            var query = new GetInvoiceDetailsQuery(999);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Return_DetailsViewModel_When_Invoice_Exists()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor
            {
                Name = "Acme Corp",
                TaxId = "9876543210",
                Address = "ul. Jasna 1, Warszawa"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Confirmed,
                Date = new DateTime(2026, 6, 14),
                ContractorId = contractor.Id,
                KsefNumber = "111-222-333",
                KsefTransactionId = "TX-999",
                KsefSentAt = new DateTime(2026, 6, 14, 12, 0, 0),
                UpoXml = "<xml>UPO</xml>",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Consulting", Quantity = 10, UnitPrice = 150m, TotalPrice = 1500m }
                }
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var handler = new GetInvoiceDetailsHandler(db);
            var query = new GetInvoiceDetailsQuery(invoice.Id);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(invoice.Id);
            result.InvoiceNumber.Should().Be("FV/2026/001");
            result.Date.Should().Be(new DateTime(2026, 6, 14));
            result.Status.Should().Be(InvoiceStatus.Confirmed);
            result.KsefNumber.Should().Be("111-222-333");
            result.KsefTransactionId.Should().Be("TX-999");
            result.KsefSentAt.Should().Be(new DateTime(2026, 6, 14, 12, 0, 0));
            result.UpoXml.Should().Be("<xml>UPO</xml>");
            
            result.Contractor.Should().NotBeNull();
            result.Contractor.Name.Should().Be("Acme Corp");
            result.Contractor.TaxId.Should().Be("9876543210");
            result.Contractor.Address.Should().Be("ul. Jasna 1, Warszawa");

            result.Items.Should().HaveCount(1);
            result.Items[0].Name.Should().Be("Consulting");
            result.Items[0].Quantity.Should().Be(10);
            result.Items[0].UnitPrice.Should().Be(150m);
            result.Items[0].TotalPrice.Should().Be(1500m);

            result.TotalAmount.Should().Be(1500m);
        }
    }
}
