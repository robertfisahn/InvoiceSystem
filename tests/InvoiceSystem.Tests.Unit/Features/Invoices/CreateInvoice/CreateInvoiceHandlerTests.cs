using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Invoices.CreateInvoice.CreateInvoiceCommand;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Invoices.CreateInvoice
{
    public class CreateInvoiceHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public CreateInvoiceHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Create_Invoice_And_Save_To_Database()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var handler = new CreateInvoiceHandler(db);

            var command = new CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = new DateTime(2026, 6, 14),
                FilePath = "/files/inv1.pdf",
                Items = new List<CreateInvoiceItemCommand>
                {
                    new CreateInvoiceItemCommand("Item A", 2, 50.00m),
                    new CreateInvoiceItemCommand("Item B", 1, 100.00m)
                }
            };

            // Act
            var resultId = await handler.Handle(command, CancellationToken.None);

            // Assert
            resultId.Should().BeGreaterThan(0);

            var invoice = await db.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == resultId);

            invoice.Should().NotBeNull();
            invoice!.ContractorId.Should().Be(contractor.Id);
            invoice.Date.Should().Be(new DateTime(2026, 6, 14));
            invoice.FilePath.Should().Be("/files/inv1.pdf");
            
            invoice.Items.Should().HaveCount(2);
            var itemsList = invoice.Items.ToList();
            itemsList[0].Name.Should().Be("Item A");
            itemsList[0].Quantity.Should().Be(2);
            itemsList[0].UnitPrice.Should().Be(50.00m);
            itemsList[0].TotalPrice.Should().Be(100.00m); // 2 * 50

            itemsList[1].Name.Should().Be("Item B");
            itemsList[1].Quantity.Should().Be(1);
            itemsList[1].UnitPrice.Should().Be(100.00m);
            itemsList[1].TotalPrice.Should().Be(100.00m); // 1 * 100
        }

        [Fact]
        public async Task Handle_Should_Generate_Sequential_Invoice_Numbers_For_Same_Year()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var handler = new CreateInvoiceHandler(db);

            var command1 = new CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = new DateTime(2026, 6, 14),
                Items = new List<CreateInvoiceItemCommand> { new CreateInvoiceItemCommand("Item", 1, 10m) }
            };

            var command2 = new CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = new DateTime(2026, 7, 20),
                Items = new List<CreateInvoiceItemCommand> { new CreateInvoiceItemCommand("Item", 1, 10m) }
            };

            // Act
            var id1 = await handler.Handle(command1, CancellationToken.None);
            var id2 = await handler.Handle(command2, CancellationToken.None);

            // Assert
            var inv1 = await db.Invoices.FindAsync(id1);
            var inv2 = await db.Invoices.FindAsync(id2);

            inv1.Should().NotBeNull();
            inv2.Should().NotBeNull();
            inv1!.InvoiceNumber.Should().Be("INV/2026/001");
            inv2!.InvoiceNumber.Should().Be("INV/2026/002");
        }

        [Fact]
        public async Task Handle_Should_Restart_Sequence_For_Different_Years()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var handler = new CreateInvoiceHandler(db);

            // First create one for 2026
            var command2026 = new CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = new DateTime(2026, 12, 31),
                Items = new List<CreateInvoiceItemCommand> { new CreateInvoiceItemCommand("Item", 1, 10m) }
            };

            // Then create one for 2027
            var command2027 = new CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = new DateTime(2027, 1, 1),
                Items = new List<CreateInvoiceItemCommand> { new CreateInvoiceItemCommand("Item", 1, 10m) }
            };

            // Act
            var id2026 = await handler.Handle(command2026, CancellationToken.None);
            var id2027 = await handler.Handle(command2027, CancellationToken.None);

            // Assert
            var inv2026 = await db.Invoices.FindAsync(id2026);
            var inv2027 = await db.Invoices.FindAsync(id2027);

            inv2026!.InvoiceNumber.Should().Be("INV/2026/001"); // First invoice of 2026 in this isolated run
            inv2027!.InvoiceNumber.Should().Be("INV/2027/001"); // First invoice of 2027
        }
    }
}
