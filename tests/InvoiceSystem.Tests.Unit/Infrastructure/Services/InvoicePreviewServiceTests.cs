using System;
using System.Collections.Generic;
using FluentAssertions;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Invoices.GetInvoiceDetails;
using InvoiceSystem.Web.Infrastructure.Ksef;
using InvoiceSystem.Web.Infrastructure.Services.Preview;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Infrastructure.Services
{
    public class InvoicePreviewServiceTests
    {
        private readonly InvoicePreviewService _service;

        public InvoicePreviewServiceTests()
        {
            _service = new InvoicePreviewService();
        }

        [Fact]
        public void MapFromInvoice_Should_Map_ViewModel_Properties_Correctly()
        {
            // Arrange
            var contractorDto = new ContractorDetailsDto("Buyer Co", "1234567890", "Address 1");
            var itemsDto = new List<InvoiceItemDto>
            {
                new InvoiceItemDto("Product A", 2, 50m, 100m)
            };
            
            var viewModel = new GetInvoiceDetailsViewModel(
                Id: 1,
                InvoiceNumber: "FV/2026/06/001",
                Date: new DateTime(2026, 6, 14),
                Contractor: contractorDto,
                Items: itemsDto,
                TotalAmount: 100m,
                Status: InvoiceStatus.Paid,
                KsefNumber: "KSEF-ABC",
                KsefTransactionId: "TRAN:XYZ123",
                KsefSentAt: new DateTime(2026, 6, 14),
                UpoXml: null
            );

            // Act
            var result = _service.MapFromInvoice(viewModel);

            // Assert
            result.Should().NotBeNull();
            result.InvoiceNumber.Should().Be("FV/2026/06/001");
            result.Date.Should().Be(viewModel.Date);
            result.KsefNumber.Should().Be("KSEF-ABC");
            result.KsefTransactionId.Should().Be("XYZ123"); // Stripped prefix
            result.StatusText.Should().Be("Opłacona");
            result.StatusClass.Should().Be("confirmed");
            result.BuyerName.Should().Be("Buyer Co");
            result.BuyerNip.Should().Be("1234567890");
            result.BuyerAddress.Should().Be("Address 1");
            result.Items.Should().ContainSingle();
            result.Items[0].Name.Should().Be("Product A");
            result.Items[0].Quantity.Should().Be(2.0);
            result.Items[0].UnitPrice.Should().Be(50m);
            result.Items[0].TotalPrice.Should().Be(100m);
            result.TotalAmount.Should().Be(100m);
        }

        [Fact]
        public void MapFromKsef_Should_Map_ParsedKsefInvoice_Correctly()
        {
            // Arrange
            var parsedItems = new List<ParsedKsefInvoiceItem>
            {
                new ParsedKsefInvoiceItem("Item K", 3, 10m, 30m)
            };
            
            var parsed = new ParsedKsefInvoice(
                InvoiceNumber: "FV/KSEF/99",
                Date: new DateTime(2026, 6, 10),
                SellerName: "Seller Name",
                SellerNip: "1111111111",
                SellerAddress: "Seller Address",
                BuyerName: "Buyer Name",
                BuyerNip: "2222222222",
                BuyerAddress: "Buyer Address",
                TotalAmount: 30m,
                Items: parsedItems
            );

            // Act
            var result = _service.MapFromKsef(parsed, "KSEF-NUM-123");

            // Assert
            result.Should().NotBeNull();
            result.InvoiceNumber.Should().Be("FV/KSEF/99");
            result.Date.Should().Be(parsed.Date);
            result.KsefNumber.Should().Be("KSEF-NUM-123");
            result.StatusText.Should().Be("Pobrana z KSeF");
            result.StatusClass.Should().Be("confirmed");
            result.SellerName.Should().Be("Seller Name");
            result.SellerNip.Should().Be("1111111111");
            result.SellerAddress.Should().Be("Seller Address");
            result.BuyerName.Should().Be("Buyer Name");
            result.BuyerNip.Should().Be("2222222222");
            result.BuyerAddress.Should().Be("Buyer Address");
            result.Items.Should().ContainSingle();
            result.Items[0].Name.Should().Be("Item K");
            result.Items[0].Quantity.Should().Be(3.0);
            result.Items[0].UnitPrice.Should().Be(10m);
            result.Items[0].TotalPrice.Should().Be(30m);
            result.TotalAmount.Should().Be(30m);
        }
    }
}
