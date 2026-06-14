using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Web.Infrastructure.Configuration;
using InvoiceSystem.Web.Infrastructure.Services.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Infrastructure.Services
{
    public class FileStorageServiceTests : IDisposable
    {
        private readonly string _tempRootPath;
        private readonly IOptions<StorageSettings> _optionsMock;
        private readonly ILogger<FileStorageService> _loggerMock;
        private readonly FileStorageService _service;

        public FileStorageServiceTests()
        {
            // Create a unique temporary directory for each test execution
            _tempRootPath = Path.Combine(Path.GetTempPath(), "InvoiceSystemTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRootPath);

            _optionsMock = Substitute.For<IOptions<StorageSettings>>();
            _optionsMock.Value.Returns(new StorageSettings { RootPath = _tempRootPath });

            _loggerMock = Substitute.For<ILogger<FileStorageService>>();
            _service = new FileStorageService(_optionsMock, _loggerMock);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRootPath))
            {
                Directory.Delete(_tempRootPath, recursive: true);
            }
        }

        [Fact]
        public async Task SaveFileAsync_Should_Save_Content_Under_DateStructured_Directory()
        {
            // Arrange
            var content = "This is sample invoice file content.";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var fileName = "invoice_123.pdf";

            // Act
            var relativePath = await _service.SaveFileAsync(stream, fileName, CancellationToken.None);

            // Assert
            relativePath.Should().NotBeNullOrEmpty();
            relativePath.Should().Contain(fileName);

            // Construct full path and verify file exists with expected content
            var fullPath = Path.Combine(_tempRootPath, relativePath);
            File.Exists(fullPath).Should().BeTrue();
            
            var savedContent = await File.ReadAllTextAsync(fullPath);
            savedContent.Should().Be(content);
        }
    }
}
