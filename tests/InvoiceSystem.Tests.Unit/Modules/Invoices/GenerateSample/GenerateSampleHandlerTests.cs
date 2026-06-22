using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Web.Modules.Invoices.Features.GenerateSample;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Invoices.GenerateSample
{
    public class GenerateSampleHandlerTests
    {
        private readonly GenerateSampleHandler _handler;

        public GenerateSampleHandlerTests()
        {
            _handler = new GenerateSampleHandler();
        }

        [Fact]
        public async Task Handle_Should_Throw_JsonException_Or_InvalidOperationException_When_Json_Is_Invalid()
        {
            // Arrange
            var command = new GenerateSampleCommand("invalid json", SampleInvoiceFormat.Pdf);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<JsonException>();
        }

        [Fact]
        public async Task Handle_Should_Generate_Pdf_Format_Successfully()
        {
            // Arrange
            var model = CreateSampleInvoiceModel();
            var json = JsonSerializer.Serialize(model);
            var command = new GenerateSampleCommand(json, SampleInvoiceFormat.Pdf);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.FileBytes.Should().NotBeNullOrEmpty();
            result.ContentType.Should().Be("application/pdf");
            result.FileName.Should().Be($"Faktura_Testowa_{model.InvoiceNumber}.pdf");
        }

        [Fact]
        public async Task Handle_Should_Generate_Jpg_Format_Successfully()
        {
            // Arrange
            var model = CreateSampleInvoiceModel();
            var json = JsonSerializer.Serialize(model);
            var command = new GenerateSampleCommand(json, SampleInvoiceFormat.Jpg);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.FileBytes.Should().NotBeNullOrEmpty();
            result.ContentType.Should().Be("image/jpeg");
            result.FileName.Should().Be($"Faktura_Testowa_{model.InvoiceNumber}.jpg");
        }

        [Fact]
        public async Task Handle_Should_Generate_Png_Format_Successfully()
        {
            // Arrange
            var model = CreateSampleInvoiceModel();
            var json = JsonSerializer.Serialize(model);
            var command = new GenerateSampleCommand(json, SampleInvoiceFormat.Png);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.FileBytes.Should().NotBeNullOrEmpty();
            result.ContentType.Should().Be("image/png");
            result.FileName.Should().Be($"Faktura_Testowa_{model.InvoiceNumber}.png");
        }

        [Fact]
        public void GenerateFakeInvoiceData_Should_Return_Populated_Model()
        {
            // Act
            var result = GenerateSampleHandler.GenerateFakeInvoiceData();

            // Assert
            result.Should().NotBeNull();
            result.InvoiceNumber.Should().StartWith("FV/");
            result.Seller.Should().NotBeNull();
            result.Seller.CompanyName.Should().Be("InvoiceSystem Enterprise");
            result.Seller.Nip.Should().Be("1234567890");
            result.Buyer.Should().NotBeNull();
            result.Buyer.CompanyName.Should().NotBeNullOrEmpty();
            result.Items.Should().NotBeEmpty();
            foreach (var item in result.Items)
            {
                item.Name.Should().NotBeNullOrEmpty();
                item.Price.Should().BeGreaterThan(0);
                item.Quantity.Should().BeGreaterThan(0);
                item.Total.Should().Be(item.Price * item.Quantity);
            }
        }

        private static SampleInvoiceModel CreateSampleInvoiceModel()
        {
            return new SampleInvoiceModel(
                "FV/2026/06/123",
                DateTime.Today,
                new SampleCompany("Seller Company", "Street 1", "City A", "11-111", "1111111111"),
                new SampleCompany("Buyer Company", "Street 2", "City B", "22-222", "2222222222"),
                new List<SampleInvoiceItem>
                {
                    new SampleInvoiceItem("Test Service", 100m, 2)
                }
            );
        }
    }
}
