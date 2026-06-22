using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Auth.Domain;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Invoices.Features.ConfirmInvoice;
using MediatR;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Invoices.ConfirmInvoice
{
    public class ConfirmInvoiceHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public ConfirmInvoiceHandlerTests()
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
            var handler = new ConfirmInvoiceHandler(db);
            var command = new ConfirmInvoiceCommand(999);

            // Act & Assert
            var act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Faktura o identyfikatorze 999 nie została znaleziona.");
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

            var handler = new ConfirmInvoiceHandler(db);
            var command = new ConfirmInvoiceCommand(invoice.Id);

            // Act & Assert
            var act = () => handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Można zatwierdzić wyłącznie faktury o statusie Szkic (Draft).");
        }

        [Fact]
        public async Task Handle_Should_Change_Status_To_Confirmed_When_Invoice_Is_Draft()
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

            var handler = new ConfirmInvoiceHandler(db);
            var command = new ConfirmInvoiceCommand(invoice.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(MediatR.Unit.Value);

            var updatedInvoice = await db.Invoices.FindAsync(invoice.Id);
            updatedInvoice.Should().NotBeNull();
            updatedInvoice!.Status.Should().Be(InvoiceStatus.Confirmed);
        }
    }
}
