using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Web.Features.Invoices.Import.ImportInvoice;
using InvoiceSystem.Web.Infrastructure.Services.Hash;
using InvoiceSystem.Web.Infrastructure.Services.Ocr;
using InvoiceSystem.Web.Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Invoices.Import.ImportInvoice
{
    public class ImportInvoiceHandlerTests
    {
        private readonly IFileStorageService _storageService;
        private readonly IFileHashService _hashService;
        private readonly IDocumentOcrService _ocrService;
        private readonly ILogger<ImportInvoiceHandler> _logger;

        public ImportInvoiceHandlerTests()
        {
            _storageService = Substitute.For<IFileStorageService>();
            _hashService = Substitute.For<IFileHashService>();
            _ocrService = Substitute.For<IDocumentOcrService>();
            _logger = Substitute.For<ILogger<ImportInvoiceHandler>>();
        }

        [Fact]
        public async Task Handle_Should_Return_Success_When_All_Steps_Succeed()
        {
            // Arrange
            var mockFile = Substitute.For<IFormFile>();
            var fileContentBytes = Encoding.UTF8.GetBytes("Mock file content");
            var fileStream = new MemoryStream(fileContentBytes);
            mockFile.OpenReadStream().Returns(fileStream);
            mockFile.FileName.Returns("test_invoice.pdf");

            _hashService.CalculateHashAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns("abc123hash");

            _storageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("uploads/2026/test_invoice.pdf");

            _ocrService.ExtractTextAsync("uploads/2026/test_invoice.pdf", Arg.Any<CancellationToken>())
                .Returns(new OcrResult("Extracted text from invoice", "PDF", true));

            var handler = new ImportInvoiceHandler(_storageService, _hashService, _ocrService, _logger);
            var command = new ImportInvoiceCommand(mockFile);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Dokument został pomyślnie przetworzony. Tekst wyodrębniony.");
            result.FilePath.Should().Be("uploads/2026/test_invoice.pdf");
            result.ExtractedText.Should().Be("Extracted text from invoice");
            result.DocumentType.Should().Be("PDF");

            await _hashService.Received(1).CalculateHashAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
            await _storageService.Received(1).SaveFileAsync(Arg.Any<Stream>(), Arg.Is<string>(s => s.EndsWith(".pdf")), Arg.Any<CancellationToken>());
            await _ocrService.Received(1).ExtractTextAsync("uploads/2026/test_invoice.pdf", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_Should_Return_Partial_Success_When_Ocr_Fails()
        {
            // Arrange
            var mockFile = Substitute.For<IFormFile>();
            var fileContentBytes = Encoding.UTF8.GetBytes("Corrupted file content");
            var fileStream = new MemoryStream(fileContentBytes);
            mockFile.OpenReadStream().Returns(fileStream);
            mockFile.FileName.Returns("bad_invoice.png");

            _hashService.CalculateHashAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns("hashval");

            _storageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("uploads/2026/bad_invoice.png");

            _ocrService.ExtractTextAsync("uploads/2026/bad_invoice.png", Arg.Any<CancellationToken>())
                .Returns(new OcrResult("", "", false, "OCR failed: service timeout"));

            var handler = new ImportInvoiceHandler(_storageService, _hashService, _ocrService, _logger);
            var command = new ImportInvoiceCommand(mockFile);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue(); // Still returns success: true because the file is saved physically
            result.Message.Should().Contain("Plik zapisany, ale ekstrakcja tekstu nie powiodła się: OCR failed: service timeout");
            result.FilePath.Should().Be("uploads/2026/bad_invoice.png");
            result.ExtractedText.Should().BeNull();
            result.DocumentType.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Exception_Is_Thrown()
        {
            // Arrange
            var mockFile = Substitute.For<IFormFile>();
            mockFile.FileName.Returns("any.pdf");
            mockFile.OpenReadStream().Throws(new Exception("Disk full or stream read error"));

            var handler = new ImportInvoiceHandler(_storageService, _hashService, _ocrService, _logger);
            var command = new ImportInvoiceCommand(mockFile);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Wystąpił błąd systemowy podczas przetwarzania dokumentu.");
            result.FilePath.Should().BeNull();
        }
    }
}
