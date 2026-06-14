using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Invoices.UpdateInvoice.UpdateInvoiceCommand;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Invoices.UpdateInvoice
{
    public class UpdateInvoiceHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public UpdateInvoiceHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Throw_InvalidOperationException_When_Invoice_Not_Found()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new UpdateInvoiceHandler(db);
            var command = new UpdateInvoiceCommand { Id = 999 };

            // Act & Assert
            var act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Invoice 999 not found.");
        }

        [Fact]
        public async Task Handle_Should_Throw_InvalidOperationException_When_Invoice_Is_Not_Draft()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Confirmed, // Not Draft
                Date = DateTime.Today,
                ContractorId = contractor.Id
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var handler = new UpdateInvoiceHandler(db);
            var command = new UpdateInvoiceCommand
            {
                Id = invoice.Id,
                ContractorId = contractor.Id,
                InvoiceNumber = "FV/2026/001-Updated",
                Date = DateTime.Today
            };

            // Act & Assert
            var act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Można edytować wyłącznie faktury o statusie Szkic (Draft).");
        }

        [Fact]
        public async Task Handle_Should_Update_Header_And_Synchronize_Items()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor1 = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            var contractor2 = new Contractor { Name = "Contractor 2", TaxId = "456", Address = "Address" };
            db.Contractors.AddRange(contractor1, contractor2);
            await db.SaveChangesAsync();
            
            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Draft,
                Date = new DateTime(2026, 6, 1),
                ContractorId = contractor1.Id,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Keep And Update Me", Quantity = 1, UnitPrice = 100m, TotalPrice = 100m },
                    new InvoiceItem { Name = "Delete Me", Quantity = 2, UnitPrice = 50m, TotalPrice = 100m }
                }
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            // Fetch auto-generated IDs
            var keepItemId = invoice.Items.First(i => i.Name == "Keep And Update Me").Id;

            var handler = new UpdateInvoiceHandler(db);
            var command = new UpdateInvoiceCommand
            {
                Id = invoice.Id,
                ContractorId = contractor2.Id, // Updated
                InvoiceNumber = "FV/2026/001-REV", // Updated
                Date = new DateTime(2026, 6, 15), // Updated
                Items = new List<UpdateInvoiceItemCommand>
                {
                    // Existing item modified
                    new UpdateInvoiceItemCommand { Id = keepItemId, Name = "Keep And Update Me (Updated)", Quantity = 3, UnitPrice = 90m },
                    // New item added (Id is null)
                    new UpdateInvoiceItemCommand { Id = null, Name = "New Added Item", Quantity = 5, UnitPrice = 20m }
                    // "Delete Me" item is omitted so it should be deleted
                }
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            var updatedInvoice = await db.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);

            updatedInvoice.Should().NotBeNull();
            updatedInvoice!.ContractorId.Should().Be(contractor2.Id);
            updatedInvoice.InvoiceNumber.Should().Be("FV/2026/001-REV");
            updatedInvoice.Date.Should().Be(new DateTime(2026, 6, 15));

            updatedInvoice.Items.Should().HaveCount(2);

            // Verify updated item
            var updatedItem = updatedInvoice.Items.FirstOrDefault(i => i.Id == keepItemId);
            updatedItem.Should().NotBeNull();
            updatedItem!.Name.Should().Be("Keep And Update Me (Updated)");
            updatedItem.Quantity.Should().Be(3);
            updatedItem.UnitPrice.Should().Be(90m);
            updatedItem.TotalPrice.Should().Be(270m); // 3 * 90

            // Verify new item
            var newItem = updatedInvoice.Items.FirstOrDefault(i => i.Id != keepItemId);
            newItem.Should().NotBeNull();
            newItem!.Name.Should().Be("New Added Item");
            newItem.Quantity.Should().Be(5);
            newItem.UnitPrice.Should().Be(20m);
            newItem.TotalPrice.Should().Be(100m); // 5 * 20
        }
    }
}
