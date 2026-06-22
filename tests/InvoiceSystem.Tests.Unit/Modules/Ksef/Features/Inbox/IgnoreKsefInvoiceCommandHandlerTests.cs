using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using InvoiceSystem.Web.Modules.Ksef.Features.Inbox.IgnoreKsefInvoice;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Ksef.Features.Inbox
{
    public class IgnoreKsefInvoiceCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public IgnoreKsefInvoiceCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Invoice_Not_Found()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new IgnoreKsefInvoiceCommandHandler(db);
            var command = new IgnoreKsefInvoiceCommand(999);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Faktura KSeF nie została znaleziona.");
        }

        [Fact]
        public async Task Handle_Should_Update_Status_To_Ignored_When_Invoice_Exists()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-123456",
                SellerName = "Seller",
                SellerNip = "1111111111",
                IssueDate = new DateTime(2026, 6, 14),
                TotalAmount = 150m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = "<xml/>"
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            var handler = new IgnoreKsefInvoiceCommandHandler(db);
            var command = new IgnoreKsefInvoiceCommand(incoming.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            using var verifyDb = _fixture.CreateContext();
            var saved = await verifyDb.KsefIncomingInvoices.FindAsync(incoming.Id);
            saved.Should().NotBeNull();
            saved!.ImportStatus.Should().Be(KsefImportStatus.Ignored);
        }
    }
}
