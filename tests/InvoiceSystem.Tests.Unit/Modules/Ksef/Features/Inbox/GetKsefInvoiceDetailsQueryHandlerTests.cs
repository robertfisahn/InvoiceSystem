using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoiceDetails;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Ksef.Features.Inbox
{
    public class GetKsefInvoiceDetailsQueryHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IKsefClient _ksefClientMock;

        public GetKsefInvoiceDetailsQueryHandlerTests()
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
            var handler = new GetKsefInvoiceDetailsQueryHandler(db, _ksefClientMock);
            var query = new GetKsefInvoiceDetailsQuery(999);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Faktura nie istnieje.");
        }

        [Fact]
        public async Task Handle_Should_Return_Details_When_RawXml_Is_Available()
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

            var handler = new GetKsefInvoiceDetailsQueryHandler(db, _ksefClientMock);
            var query = new GetKsefInvoiceDetailsQuery(incoming.Id);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.KsefNumber.Should().Be("KSEF-123456");
            result.ParsedInvoice.Should().NotBeNull();
            result.ParsedInvoice!.InvoiceNumber.Should().Be("FV/2026/06/1");
            result.ParsedInvoice!.SellerNip.Should().Be("1111111111");
            result.ParsedInvoice!.TotalAmount.Should().Be(150m);
        }

        [Fact]
        public async Task Handle_Should_Download_RawXml_When_Empty_And_Ksef_Is_Configured()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            
            // Add configuration
            var setting = new KsefSetting
            {
                Nip = "2222222222",
                ApiKey = "key",
                IsEnabled = true
            };
            db.KsefSettings.Add(setting);

            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-123456",
                SellerName = "Seller",
                SellerNip = "1111111111",
                IssueDate = new DateTime(2026, 6, 14),
                TotalAmount = 150m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = "" // Empty XML
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            // Set up mock calls
            _ksefClientMock.AuthorisationChallengeAsync("2222222222", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("challenge_123", "2026-06-14T19:00:00Z"));
            _ksefClientMock.InitSessionAsync("2222222222", "key", "challenge_123", "2026-06-14T19:00:00Z", Arg.Any<CancellationToken>())
                .Returns("session_token_xyz");
            _ksefClientMock.DownloadInvoiceXmlAsync("session_token_xyz", "KSEF-123456", Arg.Any<CancellationToken>())
                .Returns(GetValidKsefXml());

            var handler = new GetKsefInvoiceDetailsQueryHandler(db, _ksefClientMock);
            var query = new GetKsefInvoiceDetailsQuery(incoming.Id);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.ParsedInvoice.Should().NotBeNull();
            result.ParsedInvoice!.InvoiceNumber.Should().Be("FV/2026/06/1");

            // Verify XML was saved to DB
            using var verifyDb = _fixture.CreateContext();
            var saved = await verifyDb.KsefIncomingInvoices.FindAsync(incoming.Id);
            saved.Should().NotBeNull();
            saved!.RawXml.Should().Be(GetValidKsefXml());
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
