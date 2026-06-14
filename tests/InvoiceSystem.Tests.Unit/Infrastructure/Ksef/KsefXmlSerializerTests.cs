using System;
using System.Collections.Generic;
using System.Xml.Linq;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Ksef;
using FluentAssertions;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Infrastructure.Ksef
{
    public class KsefXmlSerializerTests
    {
        private static readonly XNamespace Ns = "http://crd.gov.pl/wzor/2025/06/25/13775/";

        [Fact]
        public void SerializeToFa3_Should_Clean_SellerNip_Of_NonDigits()
        {
            // Arrange
            var invoice = CreateBaseInvoice();
            string sellerNipWithHyphens = "525-23-41-234";

            // Act
            var xml = KsefXmlSerializer.SerializeToFa3(invoice, sellerNipWithHyphens);
            var doc = XDocument.Parse(xml);

            // Assert
            var sellerNip = doc.Root?.Element(Ns + "Podmiot1")?.Element(Ns + "DaneIdentyfikacyjne")?.Element(Ns + "NIP")?.Value;
            sellerNip.Should().Be("5252341234");
        }

        [Fact]
        public void SerializeToFa3_Should_Clean_ContractorNip_Of_NonDigits()
        {
            // Arrange
            var invoice = CreateBaseInvoice();
            invoice.Contractor.TaxId = "PL 954-278-99-00";

            // Act
            var xml = KsefXmlSerializer.SerializeToFa3(invoice, "1234567890");
            var doc = XDocument.Parse(xml);

            // Assert
            var buyerNip = doc.Root?.Element(Ns + "Podmiot2")?.Element(Ns + "DaneIdentyfikacyjne")?.Element(Ns + "NIP")?.Value;
            buyerNip.Should().Be("9542789900");
        }

        [Fact]
        public void SerializeToFa3_Should_Calculate_Net_Vat_And_Gross_Sums_Correctly()
        {
            // Arrange
            var invoice = CreateBaseInvoice();
            invoice.Items = new List<InvoiceItem>
            {
                new InvoiceItem { Name = "Item A", Quantity = 2, UnitPrice = 100.50m }, // 201.00 net
                new InvoiceItem { Name = "Item B", Quantity = 3, UnitPrice = 50.00m }   // 150.00 net
            }; // Total Net = 351.00, VAT 23% = 80.73, Gross = 431.73

            // Act
            var xml = KsefXmlSerializer.SerializeToFa3(invoice, "1234567890");
            var doc = XDocument.Parse(xml);

            // Assert
            var fa = doc.Root?.Element(Ns + "Fa");
            fa?.Element(Ns + "P_13_1")?.Value.Should().Be("351.00");
            fa?.Element(Ns + "P_14_1")?.Value.Should().Be("80.73");
            fa?.Element(Ns + "P_15")?.Value.Should().Be("431.73");
        }

        [Theory]
        [InlineData("ul. Marszałkowska 10/12\n00-100 Warszawa", "ul. Marszałkowska 10/12", "00-100 Warszawa")]
        [InlineData("Aleje Jerozolimskie 45, 02-005 Warszawa, Polska", "Aleje Jerozolimskie 45", "02-005 Warszawa, Polska")]
        public void SerializeToFa3_Should_Parse_Address_With_Postcode_Into_AdresL1_And_AdresL2(
            string rawAddress, string expectedL1, string expectedL2)
        {
            // Arrange
            var invoice = CreateBaseInvoice();
            invoice.Contractor.Address = rawAddress;

            // Act
            var xml = KsefXmlSerializer.SerializeToFa3(invoice, "1234567890");
            var doc = XDocument.Parse(xml);

            // Assert
            var buyerAddress = doc.Root?.Element(Ns + "Podmiot2")?.Element(Ns + "Adres");
            buyerAddress.Should().NotBeNull();
            buyerAddress?.Element(Ns + "AdresL1")?.Value.Should().Be(expectedL1);
            buyerAddress?.Element(Ns + "AdresL2")?.Value.Should().Be(expectedL2);
        }

        [Fact]
        public void SerializeToFa3_Should_Parse_Address_Without_Postcode_Into_AdresL1_Only()
        {
            // Arrange
            var invoice = CreateBaseInvoice();
            invoice.Contractor.Address = "ul. Kwiatowa 5 Bez Kodu Pocztowego";

            // Act
            var xml = KsefXmlSerializer.SerializeToFa3(invoice, "1234567890");
            var doc = XDocument.Parse(xml);

            // Assert
            var buyerAddress = doc.Root?.Element(Ns + "Podmiot2")?.Element(Ns + "Adres");
            buyerAddress.Should().NotBeNull();
            buyerAddress?.Element(Ns + "AdresL1")?.Value.Should().Be("ul. Kwiatowa 5 Bez Kodu Pocztowego");
            buyerAddress?.Element(Ns + "AdresL2").Should().BeNull();
        }

        [Fact]
        public void SerializeToFa3_Should_Parse_Address_Starting_With_Postcode_Into_AdresL1_Only()
        {
            // Arrange
            var invoice = CreateBaseInvoice();
            invoice.Contractor.Address = "00-100 Warszawa";

            // Act
            var xml = KsefXmlSerializer.SerializeToFa3(invoice, "1234567890");
            var doc = XDocument.Parse(xml);

            // Assert
            var buyerAddress = doc.Root?.Element(Ns + "Podmiot2")?.Element(Ns + "Adres");
            buyerAddress.Should().NotBeNull();
            buyerAddress?.Element(Ns + "AdresL1")?.Value.Should().Be("00-100 Warszawa");
            buyerAddress?.Element(Ns + "AdresL2").Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SerializeToFa3_Should_Skip_Buyer_Address_When_Address_Is_Null_Or_Empty(string? invalidAddress)
        {
            // Arrange
            var invoice = CreateBaseInvoice();
            invoice.Contractor.Address = invalidAddress;

            // Act
            var xml = KsefXmlSerializer.SerializeToFa3(invoice, "1234567890");
            var doc = XDocument.Parse(xml);

            // Assert
            var buyerAddress = doc.Root?.Element(Ns + "Podmiot2")?.Element(Ns + "Adres");
            buyerAddress.Should().BeNull();
        }

        private Invoice CreateBaseInvoice()
        {
            return new Invoice
            {
                InvoiceNumber = "FV/2026/06/001",
                Date = new DateTime(2026, 6, 14),
                Contractor = new Contractor
                {
                    Name = "Buyer Co",
                    TaxId = "1234567890",
                    Address = "ul. Widok 5\n12-345 Miasto"
                },
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Default Product", Quantity = 1, UnitPrice = 100.00m }
                }
            };
        }
    }
}
