using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Auth.Domain;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Invoices.Features.Import;
using InvoiceSystem.Web.Modules.Invoices.Features.Import.ConfirmOcr;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Invoices.Import.ConfirmOcr
{
    public class ConfirmOcrInvoiceCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IMemoryCache _cache;

        public ConfirmOcrInvoiceCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public void Dispose()
        {
            _fixture.Dispose();
            _cache.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Session_Not_Found_In_Cache()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new ConfirmOcrInvoiceCommandHandler(db, _cache);
            var command = new ConfirmOcrInvoiceCommand(
                "invalid-session",
                "Buyer",
                "123",
                "Addr",
                DateTime.Today,
                new List<ConfirmOcrItemCommand>()
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ContractorExists.Should().BeFalse();
            result.CreateInvoiceCommandJson.Should().BeNull();
            result.ErrorMessage.Should().Be("Sesja analizy OCR wygasła lub jest nieprawidłowa. Prześlij plik ponownie.");
        }

        [Fact]
        public async Task Handle_Should_Return_ContractorExists_True_And_CreateInvoiceCommandJson_When_Contractor_Exists()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Existing Contractor", TaxId = "1234567890", Address = "Tax Street" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            const string sessionId = "valid-session-id";
            var sessionData = new OcrSessionData
            {
                FilePath = "uploads/invoice.pdf",
                BuyerName = "Old Name",
                BuyerTaxId = "99999",
                BuyerAddress = "Old Addr",
                Date = DateTime.Today.AddDays(-10),
                Items = new List<OcrSessionItem>
                {
                    new OcrSessionItem { Name = "Old Item", Quantity = 1, UnitPrice = 100m }
                }
            };
            _cache.Set($"ocr-session-{sessionId}", sessionData);

            var handler = new ConfirmOcrInvoiceCommandHandler(db, _cache);
            
            var itemsCommand = new List<ConfirmOcrItemCommand>
            {
                new ConfirmOcrItemCommand("New Item", 5, 20m)
            };
            var command = new ConfirmOcrInvoiceCommand(
                sessionId,
                "Existing Contractor",
                "123-456-78-90", // cleans to 1234567890 (matches contractor)
                "Tax Street",
                DateTime.Today,
                itemsCommand
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ContractorExists.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.CreateInvoiceCommandJson.Should().NotBeNull();

            // Verify the generated JSON matches the CreateInvoiceCommand format
            var parsedCommand = JsonSerializer.Deserialize<InvoiceSystem.Web.Modules.Invoices.Features.CreateInvoice.CreateInvoiceCommand.CreateInvoiceCommand>(result.CreateInvoiceCommandJson!);
            parsedCommand.Should().NotBeNull();
            parsedCommand!.ContractorId.Should().Be(contractor.Id);
            parsedCommand.Date.Should().Be(DateTime.Today);
            parsedCommand.FilePath.Should().Be("uploads/invoice.pdf");
            parsedCommand.Items.Should().HaveCount(1);
            parsedCommand.Items[0].Name.Should().Be("New Item");
            parsedCommand.Items[0].Quantity.Should().Be(5);
            parsedCommand.Items[0].UnitPrice.Should().Be(20m);

            // Verify cache session was removed since the checkout completed
            _cache.TryGetValue($"ocr-session-{sessionId}", out _).Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_Return_ContractorExists_False_When_Contractor_Not_Found_In_Db()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            const string sessionId = "valid-session-id-unregistered";
            var sessionData = new OcrSessionData
            {
                FilePath = "uploads/invoice.pdf",
                BuyerName = "Unregistered",
                BuyerTaxId = "1111111111",
                BuyerAddress = "Nowhere",
                Date = DateTime.Today
            };
            _cache.Set($"ocr-session-{sessionId}", sessionData);

            var handler = new ConfirmOcrInvoiceCommandHandler(db, _cache);
            var command = new ConfirmOcrInvoiceCommand(
                sessionId,
                "New Unregistered Contractor",
                "111-111-11-11", // cleans to 1111111111
                "Nowhere",
                DateTime.Today,
                new List<ConfirmOcrItemCommand>()
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ContractorExists.Should().BeFalse();
            result.CreateInvoiceCommandJson.Should().BeNull();
            result.ErrorMessage.Should().BeNull();

            // Verify session data was updated in the cache for registration flow
            _cache.TryGetValue<OcrSessionData>($"ocr-session-{sessionId}", out var cachedData).Should().BeTrue();
            cachedData.Should().NotBeNull();
            cachedData!.BuyerName.Should().Be("New Unregistered Contractor");
            cachedData.BuyerTaxId.Should().Be("1111111111");
        }
    }
}
