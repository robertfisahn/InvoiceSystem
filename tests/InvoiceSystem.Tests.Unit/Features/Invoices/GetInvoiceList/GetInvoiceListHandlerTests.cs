using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Invoices.GetInvoiceList;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Invoices.GetInvoiceList
{
    public class GetInvoiceListHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public GetInvoiceListHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_List_When_No_Invoices_Exist()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new GetInvoiceListHandler(db);
            var query = new GetInvoiceListQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_Return_List_Of_ViewModels_When_Invoices_Exist()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Client A", TaxId = "111", Address = "Addr A" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice1 = new Invoice
            {
                InvoiceNumber = "INV-001",
                Date = new DateTime(2026, 6, 1),
                ContractorId = contractor.Id,
                Status = InvoiceStatus.Draft,
                KsefNumber = "KSEF-1",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Item A", Quantity = 2, UnitPrice = 10m, TotalPrice = 20m }
                }
            };
            var invoice2 = new Invoice
            {
                InvoiceNumber = "INV-002",
                Date = new DateTime(2026, 6, 2),
                ContractorId = contractor.Id,
                Status = InvoiceStatus.Confirmed,
                KsefNumber = null,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Item B", Quantity = 1, UnitPrice = 50m, TotalPrice = 50m }
                }
            };
            db.Invoices.AddRange(invoice1, invoice2);
            await db.SaveChangesAsync();

            var handler = new GetInvoiceListHandler(db);
            var query = new GetInvoiceListQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);

            var vm1 = result.Find(x => x.Id == invoice1.Id);
            vm1.Should().NotBeNull();
            vm1!.InvoiceNumber.Should().Be("INV-001");
            vm1.ContractorName.Should().Be("Client A");
            vm1.Date.Should().Be(new DateTime(2026, 6, 1));
            vm1.TotalAmount.Should().Be(20m);
            vm1.Status.Should().Be(InvoiceStatus.Draft);
            vm1.KsefNumber.Should().Be("KSEF-1");

            var vm2 = result.Find(x => x.Id == invoice2.Id);
            vm2.Should().NotBeNull();
            vm2!.InvoiceNumber.Should().Be("INV-002");
            vm2.ContractorName.Should().Be("Client A");
            vm2.Date.Should().Be(new DateTime(2026, 6, 2));
            vm2.TotalAmount.Should().Be(50m);
            vm2.Status.Should().Be(InvoiceStatus.Confirmed);
            vm2.KsefNumber.Should().BeNull();
        }
    }
}
