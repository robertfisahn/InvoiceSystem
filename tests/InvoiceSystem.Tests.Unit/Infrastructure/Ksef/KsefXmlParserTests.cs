using System;
using System.Collections.Generic;
using System.Xml;
using FluentAssertions;
using InvoiceSystem.Web.Infrastructure.Ksef;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Infrastructure.Ksef
{
    public class KsefXmlParserTests
    {
        [Fact]
        public void ParseFa2_Should_Throw_ArgumentException_When_Root_Or_Document_Is_Invalid()
        {
            // Arrange
            var xml = "<root></root>";

            // Act & Assert
            var act = () => KsefXmlParser.ParseFa2(xml);
            act.Should().Throw<ArgumentException>().WithMessage("Missing 'Fa' element in XML.");
        }

        [Fact]
        public void ParseFa2_Should_Throw_XmlException_When_Xml_Is_Malformed()
        {
            // Arrange
            var xml = "<invalid xml";

            // Act & Assert
            var act = () => KsefXmlParser.ParseFa2(xml);
            act.Should().Throw<XmlException>();
        }

        [Fact]
        public void ParseFa2_Should_Parse_All_Fields_Correctly_With_AdresPol()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2025/06/25/13775/"">
    <Podmiot1>
        <DanePodmiotu>
            <NIP>1112223344</NIP>
            <Nazwa>Seller Sp. z o.o.</Nazwa>
        </DanePodmiotu>
        <Adres>
            <AdresPol>
                <Ulica>ul. Warszawska</Ulica>
                <NrDomu>10A</NrDomu>
                <KodPocztowy>00-100</KodPocztowy>
                <Miejscowosc>Warszawa</Miejscowosc>
            </AdresPol>
        </Adres>
    </Podmiot1>
    <Podmiot2>
        <DanePodmiotu>
            <NIP>5556667788</NIP>
            <Nazwa>Buyer S.A.</Nazwa>
        </DanePodmiotu>
        <Adres>
            <AdresPol>
                <Ulica>ul. Krakowska</Ulica>
                <NrDomu>5</NrDomu>
                <KodPocztowy>30-001</KodPocztowy>
                <Miejscowosc>Kraków</Miejscowosc>
            </AdresPol>
        </Adres>
    </Podmiot2>
    <Fa>
        <P_1>2026-06-14</P_1>
        <P_2>FV/2026/001</P_2>
        <P_15>123.45</P_15>
        <FaWiersz>
            <P_7>Item 1</P_7>
            <P_8A>2</P_8A>
            <P_9B>50.00</P_9B>
            <P_11>100.00</P_11>
        </FaWiersz>
    </Fa>
</Faktura>";

            // Act
            var result = KsefXmlParser.ParseFa2(xml);

            // Assert
            result.Should().NotBeNull();
            result.InvoiceNumber.Should().Be("FV/2026/001");
            result.Date.Should().Be(new DateTime(2026, 6, 14));
            result.TotalAmount.Should().Be(123.45m);

            result.SellerNip.Should().Be("1112223344");
            result.SellerName.Should().Be("Seller Sp. z o.o.");
            result.SellerAddress.Should().Be("ul. Warszawska 10A, 00-100 Warszawa");

            result.BuyerNip.Should().Be("5556667788");
            result.BuyerName.Should().Be("Buyer S.A.");
            result.BuyerAddress.Should().Be("ul. Krakowska 5, 30-001 Kraków");

            result.Items.Should().HaveCount(1);
            result.Items[0].Name.Should().Be("Item 1");
            result.Items[0].Quantity.Should().Be(2);
            result.Items[0].UnitPrice.Should().Be(50.00m);
            result.Items[0].TotalPrice.Should().Be(100.00m);
        }

        [Fact]
        public void ParseFa2_Should_Parse_AdresLnk_And_OsobaFizyczna_And_Compute_TotalPrice()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2025/06/25/13775/"">
    <Podmiot1>
        <DanePodmiotu>
            <NIP>1112223344</NIP>
            <Nazwa>Seller Sp. z o.o.</Nazwa>
        </DanePodmiotu>
        <Adres>
            <AdresLnk>
                <AdresTekst>Some foreign seller address 123</AdresTekst>
            </AdresLnk>
        </Adres>
    </Podmiot1>
    <Podmiot2>
        <DanePodmiotu>
            <OsobaFizyczna>
                <ImiePierwsze>Jan</ImiePierwsze>
                <Nazwisko>Kowalski</Nazwisko>
            </OsobaFizyczna>
        </DanePodmiotu>
        <Adres>
            <AdresLnk>
                <AdresTekst>Some foreign buyer address 456</AdresTekst>
            </AdresLnk>
        </Adres>
    </Podmiot2>
    <Fa>
        <P_1>2026-06-14</P_1>
        <P_2>FV/2026/002</P_2>
        <P_15>150.00</P_15>
        <FaWiersz>
            <P_7>Item Without TotalPrice Element</P_7>
            <P_8A>3</P_8A>
            <P_9B>50.00</P_9B>
        </FaWiersz>
    </Fa>
</Faktura>";

            // Act
            var result = KsefXmlParser.ParseFa2(xml);

            // Assert
            result.Should().NotBeNull();
            result.SellerAddress.Should().Be("Some foreign seller address 123");
            result.BuyerName.Should().Be("Jan Kowalski");
            result.BuyerAddress.Should().Be("Some foreign buyer address 456");

            result.Items.Should().HaveCount(1);
            result.Items[0].Name.Should().Be("Item Without TotalPrice Element");
            result.Items[0].Quantity.Should().Be(3);
            result.Items[0].UnitPrice.Should().Be(50.00m);
            // totalPrice is computed as qty * price (3 * 50 = 150)
            result.Items[0].TotalPrice.Should().Be(150.00m);
        }

        [Fact]
        public void ParseFa2_Should_Handle_Empty_And_Missing_Address_Components()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2025/06/25/13775/"">
    <Podmiot1>
        <Adres>
            <AdresPol>
                <Miejscowosc>Gdynia</Miejscowosc>
            </AdresPol>
        </Adres>
    </Podmiot1>
    <Podmiot2>
        <Adres>
            <AdresPol>
                <Ulica>ul. Cicha</Ulica>
            </AdresPol>
        </Adres>
    </Podmiot2>
    <Fa>
        <P_1>2026-06-14</P_1>
        <P_2>FV/2026/003</P_2>
        <P_15>0.00</P_15>
        <FaWiersz>
            <!-- Missing name and quantity less than 1 -->
            <P_8A>-5</P_8A>
        </FaWiersz>
    </Fa>
</Faktura>";

            // Act
            var result = KsefXmlParser.ParseFa2(xml);

            // Assert
            result.Should().NotBeNull();
            result.SellerAddress.Should().Be("Gdynia");
            result.BuyerAddress.Should().Be("ul. Cicha");
            
            result.Items.Should().HaveCount(1);
            result.Items[0].Name.Should().Be("Pozycja");
            // qty should be normalized to 1
            result.Items[0].Quantity.Should().Be(1);
            result.Items[0].UnitPrice.Should().Be(0m);
            result.Items[0].TotalPrice.Should().Be(0m);
        }

        [Fact]
        public void ParseFa2_Should_Handle_Missing_Optional_Elements_And_Return_Defaults()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2025/06/25/13775/"">
    <Fa>
        <P_1>invalid-date</P_1>
        <P_2></P_2>
        <P_15>invalid-amount</P_15>
    </Fa>
</Faktura>";

            // Act
            var result = KsefXmlParser.ParseFa2(xml);

            // Assert
            result.Should().NotBeNull();
            result.InvoiceNumber.Should().BeEmpty();
            result.Date.Should().Be(default);
            result.TotalAmount.Should().Be(0m);
            result.SellerName.Should().BeEmpty();
            result.SellerNip.Should().BeEmpty();
            result.SellerAddress.Should().BeEmpty();
            result.BuyerName.Should().BeEmpty();
            result.BuyerNip.Should().BeEmpty();
            result.BuyerAddress.Should().BeEmpty();
            result.Items.Should().BeEmpty();
        }
    }
}
