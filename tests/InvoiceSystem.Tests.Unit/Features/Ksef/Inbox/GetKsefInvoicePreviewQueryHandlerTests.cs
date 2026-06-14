using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoicePreview;
using InvoiceSystem.Web.Infrastructure.Ksef;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Ksef.Inbox.GetKsefInvoicePreview
{
    public class GetKsefInvoicePreviewQueryHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IKsefClient _ksefClientMock;

        public GetKsefInvoicePreviewQueryHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _ksefClientMock = Substitute.For<IKsefClient>();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Invoice_Not_Found()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new GetKsefInvoicePreviewQueryHandler(db, _ksefClientMock);
            var query = new GetKsefInvoicePreviewQuery(999);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Faktura nie istnieje.");
        }

        [Fact]
        public async Task Handle_Should_Return_Preview_When_RawXml_Is_Available()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-123456",
                SellerName = "Seller",
                SellerNip = "1111111111",
                IssueDate = new DateTime(2026, 6, 14),
                TotalAmount = 150m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = GetValidKsefXml()
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            var handler = new GetKsefInvoicePreviewQueryHandler(db, _ksefClientMock);
            var query = new GetKsefInvoicePreviewQuery(incoming.Id);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.InvoiceNumber.Should().Be("FV/2026/06/1");
            result.SellerName.Should().Be("Seller Name");
            result.SellerNip.Should().Be("1111111111");
            result.BuyerName.Should().Be("Buyer Name");
            result.BuyerNip.Should().Be("2222222222");
            result.TotalAmount.Should().Be(150m);
            result.Items.Should().ContainSingle();
            result.Items[0].Name.Should().Be("Service A");
            result.Items[0].Quantity.Should().Be(1);
            result.Items[0].UnitPrice.Should().Be(150m);
            result.Items[0].TotalPrice.Should().Be(150m);
        }

        private static string GetValidKsefXml()
        {
            return @"<Faktura>
  <Podmiot1>
    <DanePodmiotu>
      <NIP>1111111111</NIP>
      <Nazwa>Seller Name</Nazwa>
    </DanePodmiotu>
    <Adres>
      <AdresPol>
        <Ulica>Street Seller</Ulica>
        <NrDomu>10</NrDomu>
        <KodPocztowy>11-111</KodPocztowy>
        <Miejscowosc>City Seller</Miejscowosc>
      </AdresPol>
    </Adres>
  </Podmiot1>
  <Podmiot2>
    <DanePodmiotu>
      <NIP>2222222222</NIP>
      <Nazwa>Buyer Name</Nazwa>
    </DanePodmiotu>
    <Adres>
      <AdresPol>
        <Ulica>Street Buyer</Ulica>
        <NrDomu>20</NrDomu>
        <KodPocztowy>22-222</KodPocztowy>
        <Miejscowosc>City Buyer</Miejscowosc>
      </AdresPol>
    </Adres>
  </Podmiot2>
  <Fa>
    <P_1>2026-06-14</P_1>
    <P_2>FV/2026/06/1</P_2>
    <P_15>150.00</P_15>
    <FaWiersz>
      <P_7>Service A</P_7>
      <P_8A>1</P_8A>
      <P_9B>150.00</P_9B>
      <P_11>150.00</P_11>
    </FaWiersz>
  </Fa>
</Faktura>";
        }
    }
}
