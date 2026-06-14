using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Invoices.Import.AnalyzeOcr;
using InvoiceSystem.Web.Infrastructure.Services.Llm;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Invoices.Import.AnalyzeOcr
{
    public class AnalyzeOcrTextHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly ILlmService _llmService;
        private readonly ILogger<AnalyzeOcrTextHandler> _logger;

        public AnalyzeOcrTextHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _llmService = Substitute.For<ILlmService>();
            _logger = Substitute.For<ILogger<AnalyzeOcrTextHandler>>();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Llm_Returns_Null()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _llmService.ExtractInvoiceDataAsync("raw text", "openai", Arg.Any<CancellationToken>())
                .Returns((LlmInvoiceDto?)null);

            var handler = new AnalyzeOcrTextHandler(_llmService, db, _logger);
            var command = new AnalyzeOcrTextCommand("raw text", "openai");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Nie udało się poprawnie sparsować faktury. Upewnij się, że tekst zawiera poprawne dane faktury.");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_TaxId_Is_Empty()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var mockData = new LlmInvoiceDto(
                "Acme",
                "", // Empty TaxId
                "Address",
                "FV/123",
                DateTime.Today,
                new List<LlmInvoiceItemDto>()
            );

            _llmService.ExtractInvoiceDataAsync("raw text", "openai", Arg.Any<CancellationToken>())
                .Returns(mockData);

            var handler = new AnalyzeOcrTextHandler(_llmService, db, _logger);
            var command = new AnalyzeOcrTextCommand("raw text", "openai");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("NIP kupującego jest wymagany do identyfikacji i przypisania kontrahenta w systemie.");
        }

        [Fact]
        public async Task Handle_Should_Return_ContractorExists_True_When_Contractor_Found_By_TaxId()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Acme Corp", TaxId = "1234567890", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var mockData = new LlmInvoiceDto(
                "Acme Corp",
                "123-456-78-90", // will clean to 1234567890
                "Address",
                "FV/123",
                DateTime.Today,
                new List<LlmInvoiceItemDto>()
            );

            _llmService.ExtractInvoiceDataAsync("raw text", "openai", Arg.Any<CancellationToken>())
                .Returns(mockData);

            var handler = new AnalyzeOcrTextHandler(_llmService, db, _logger);
            var command = new AnalyzeOcrTextCommand("raw text", "openai");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ContractorExists.Should().BeTrue();
            result.ContractorId.Should().Be(contractor.Id);
            result.Data.Should().Be(mockData);
        }

        [Fact]
        public async Task Handle_Should_Return_ContractorExists_False_When_Contractor_Not_Found()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var mockData = new LlmInvoiceDto(
                "New Corp",
                "9998887766",
                "New Address",
                "FV/123",
                DateTime.Today,
                new List<LlmInvoiceItemDto>()
            );

            _llmService.ExtractInvoiceDataAsync("raw text", "openai", Arg.Any<CancellationToken>())
                .Returns(mockData);

            var handler = new AnalyzeOcrTextHandler(_llmService, db, _logger);
            var command = new AnalyzeOcrTextCommand("raw text", "openai");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ContractorExists.Should().BeFalse();
            result.ContractorId.Should().BeNull();
            result.Data.Should().Be(mockData);
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Exception_Is_Thrown()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _llmService.ExtractInvoiceDataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception("LLM provider down"));

            var handler = new AnalyzeOcrTextHandler(_llmService, db, _logger);
            var command = new AnalyzeOcrTextCommand("raw text", "openai");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Błąd serwera analizy: LLM provider down");
        }
    }
}
