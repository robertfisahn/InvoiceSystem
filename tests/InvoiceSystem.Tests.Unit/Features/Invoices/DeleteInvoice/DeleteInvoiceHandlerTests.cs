using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Invoices.DeleteInvoice;
using MediatR;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Invoices.DeleteInvoice
{
    public class DeleteInvoiceHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public DeleteInvoiceHandlerTests()
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
            var handler = new DeleteInvoiceHandler(db);
            var command = new DeleteInvoiceCommand(999);

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

            var handler = new DeleteInvoiceHandler(db);
            var command = new DeleteInvoiceCommand(invoice.Id);

            // Act & Assert
            var act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Można usuwać wyłącznie faktury o statusie Szkic (Draft).");
        }

        [Fact]
        public async Task Handle_Should_Delete_Invoice_When_Invoice_Is_Draft()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Draft,
                Date = DateTime.Today,
                ContractorId = contractor.Id
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var handler = new DeleteInvoiceHandler(db);
            var command = new DeleteInvoiceCommand(invoice.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(MediatR.Unit.Value);

            var deletedInvoice = await db.Invoices.FindAsync(invoice.Id);
            deletedInvoice.Should().BeNull();
        }
    }
}
