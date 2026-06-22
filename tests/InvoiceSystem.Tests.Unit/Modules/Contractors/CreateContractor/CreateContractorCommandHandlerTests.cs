using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Contractors.Features.CreateContractor.CreateContractorCommand;
using InvoiceSystem.Web.Modules.Invoices.Features.Import;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Contractors.CreateContractor
{
    public class CreateContractorCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CreateContractorCommandHandler> _logger;

        public CreateContractorCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _logger = Substitute.For<ILogger<CreateContractorCommandHandler>>();
        }

        public void Dispose()
        {
            _fixture.Dispose();
            _cache.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Create_New_Contractor_When_TaxId_Not_Exists()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new CreateContractorCommandHandler(db, _cache, _logger);
            var command = new CreateContractorCommand
            {
                Name = "  Acme Corp  ",
                TaxId = "123-456-78-90",
                Address = "  ul. Jasna 10, Warszawa  "
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ContractorId.Should().BeGreaterThan(0);
            result.ErrorMessage.Should().BeNull();
            result.CreateInvoiceCommandJson.Should().BeNull();

            // Verify db entity has trimmed fields and cleaned tax ID
            var contractor = await db.Contractors.FindAsync(result.ContractorId);
            contractor.Should().NotBeNull();
            contractor!.Name.Should().Be("Acme Corp");
            contractor.TaxId.Should().Be("1234567890"); // hyphens removed
            contractor.Address.Should().Be("ul. Jasna 10, Warszawa");
        }

        [Fact]
        public async Task Handle_Should_Return_Existing_Contractor_When_TaxId_Exists()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var existing = new Contractor
            {
                Name = "Existing Inc",
                TaxId = "9998887766",
                Address = "Old Address"
            };
            db.Contractors.Add(existing);
            await db.SaveChangesAsync();

            var handler = new CreateContractorCommandHandler(db, _cache, _logger);
            var command = new CreateContractorCommand
            {
                Name = "Existing Inc - New Name Attempt",
                TaxId = "999-888-77-66", // cleaned is 9998887766
                Address = "New Address"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ContractorId.Should().Be(existing.Id);
            result.ErrorMessage.Should().BeNull();
            result.CreateInvoiceCommandJson.Should().BeNull();

            // Verify Contractor was NOT modified in db
            var dbContractor = await db.Contractors.FindAsync(existing.Id);
            dbContractor!.Name.Should().Be("Existing Inc");
            dbContractor.Address.Should().Be("Old Address");
        }

        [Fact]
        public async Task Handle_Should_Process_OcrSession_When_SessionId_Is_Provided_And_Found_In_Cache()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            
            const string sessionId = "test-session-xyz";
            var ocrData = new OcrSessionData
            {
                FilePath = "/invoices/ocr123.pdf",
                BuyerName = "Acme",
                BuyerTaxId = "1234567890",
                BuyerAddress = "Address",
                Date = new DateTime(2026, 6, 14),
                Items = new List<OcrSessionItem>
                {
                    new OcrSessionItem { Name = "Item A", Quantity = 2, UnitPrice = 50.00m }
                }
            };

            _cache.Set($"ocr-session-{sessionId}", ocrData);

            var handler = new CreateContractorCommandHandler(db, _cache, _logger);
            var command = new CreateContractorCommand
            {
                SessionId = sessionId,
                Name = "Acme Corp",
                TaxId = "1234567890",
                Address = "Address"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.CreateInvoiceCommandJson.Should().NotBeNull();

            // Verify json content
            var deserialized = JsonSerializer.Deserialize<JsonElement>(result.CreateInvoiceCommandJson!);
            deserialized.GetProperty("ContractorId").GetInt32().Should().Be(result.ContractorId);
            deserialized.GetProperty("Date").GetDateTime().Should().Be(new DateTime(2026, 6, 14));
            deserialized.GetProperty("FilePath").GetString().Should().Be("/invoices/ocr123.pdf");
            
            var items = deserialized.GetProperty("Items");
            items.GetArrayLength().Should().Be(1);
            items[0].GetProperty("Name").GetString().Should().Be("Item A");
            items[0].GetProperty("Quantity").GetInt32().Should().Be(2);
            items[0].GetProperty("UnitPrice").GetDecimal().Should().Be(50.00m);

            // Verify ocr data was removed from cache
            _cache.TryGetValue($"ocr-session-{sessionId}", out _).Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_Throw_ObjectDisposedException_When_Context_Is_Disposed()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            
            var handler = new CreateContractorCommandHandler(db, _cache, _logger);
            var command = new CreateContractorCommand
            {
                Name = "Acme",
                TaxId = "1234567890"
            };

            db.Dispose(); // Forces exception on connection

            // Act
            var act = () => handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }
    }
}
