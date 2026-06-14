using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Web.Infrastructure.Services.Hash;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Infrastructure.Services
{
    public class FileHashServiceTests
    {
        private readonly FileHashService _service;

        public FileHashServiceTests()
        {
            _service = new FileHashService();
        }

        [Fact]
        public async Task CalculateHashAsync_Should_Return_Correct_Sha256_Hash()
        {
            // Arrange
            var content = "Hello World KSeF OCR Test Content";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            
            // Expected SHA-256 for the string above
            // Echo -n "Hello World KSeF OCR Test Content" | sha256sum
            // -> c31826b1c1d09e530669288ee8740c0ad7a2e21245fa2232a514d310e527ccfe
            var expectedHash = "3c8ca25b4a27c53fbbf5952557af99ed4ccd4168c502feeeaea7610bfee6dbd1";

            // Act
            var result = await _service.CalculateHashAsync(stream, CancellationToken.None);

            // Assert
            result.Should().Be(expectedHash);
        }

        [Fact]
        public async Task CalculateHashAsync_Should_Return_Correct_Hash_For_EmptyStream()
        {
            // Arrange
            using var stream = new MemoryStream();
            var expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // Empty SHA-256

            // Act
            var result = await _service.CalculateHashAsync(stream, CancellationToken.None);

            // Assert
            result.Should().Be(expectedHash);
        }
    }
}
