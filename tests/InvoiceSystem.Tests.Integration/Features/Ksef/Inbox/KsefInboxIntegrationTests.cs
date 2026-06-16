using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceDetails;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoicePreview;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceXml;
using InvoiceSystem.Web.Features.Ksef.Inbox.SyncKsefInbox;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Ksef.Inbox
{
    public class KsefInboxIntegrationTests : IntegrationTestBase
    {
        private async Task PrepareKsefSettingsAsync(AppDbContext db)
        {
            var setting = new KsefSetting
            {
                Nip = "1234567890",
                ApiKey = "key-123",
                LastSyncDate = null
            };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task SyncKsefInbox_ShouldSyncInvoicesAndSaveRawXml_WhenSuccessful()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            await PrepareKsefSettingsAsync(db);

            // Setup mock client
            testKsefClient.ShouldThrow = false;
            testKsefClient.ExceptionToThrow = null;
            testKsefClient.IncomingInvoicesToReturn = new List<KsefIncomingInvoiceDto>
            {
                new KsefIncomingInvoiceDto(
                    "1234567890-20260616-111111-ABCDEF",
                    "Test Seller Ltd",
                    "1112223344",
                    new DateTime(2026, 6, 16),
                    123.45m,
                    ""
                )
            };
            testKsefClient.XmlToReturn = GetValidKsefXml();

            var command = new SyncKsefInboxCommand();

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.NewInvoicesCount.Should().Be(1);
            result.ErrorMessage.Should().BeNull();

            // Verify DB state
            var savedInvoice = await db.KsefIncomingInvoices.FirstOrDefaultAsync(i => i.KsefNumber == "1234567890-20260616-111111-ABCDEF");
            savedInvoice.Should().NotBeNull();
            savedInvoice!.SellerName.Should().Be("Test Seller Ltd");
            savedInvoice.SellerNip.Should().Be("1112223344");
            savedInvoice.TotalAmount.Should().Be(123.45m);
            savedInvoice.RawXml.Should().Be(GetValidKsefXml());
            savedInvoice.ImportStatus.Should().Be(KsefImportStatus.Pending);

            // Verify settings LastSyncDate updated
            var settings = await db.KsefSettings.FirstAsync();
            settings.LastSyncDate.Should().NotBeNull();
            settings.ActiveSessionToken.Should().Be("mock_token|ref|aes|iv|online_ref_999");
        }

        [Fact]
        public async Task SyncKsefInbox_ShouldFailAndAbort_WhenXmlDownloadReturns429TooManyRequests()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            await PrepareKsefSettingsAsync(db);

            // Setup mock client
            testKsefClient.ShouldThrow = false;
            testKsefClient.IncomingInvoicesToReturn = new List<KsefIncomingInvoiceDto>
            {
                new KsefIncomingInvoiceDto(
                    "1234567890-20260616-111111-ABCDEF",
                    "Test Seller Ltd",
                    "1112223344",
                    new DateTime(2026, 6, 16),
                    123.45m,
                    ""
                )
            };
            
            // Set up XML download exception to throw 429
            testKsefClient.DownloadXmlExceptionToThrow = new HttpRequestException(
                "Too many requests",
                null,
                HttpStatusCode.TooManyRequests
            );

            var command = new SyncKsefInboxCommand();

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.NewInvoicesCount.Should().Be(0);
            result.ErrorMessage.Should().Contain("limit");

            // Verify no invoices saved
            var savedCount = await db.KsefIncomingInvoices.CountAsync();
            savedCount.Should().Be(0);

            // Verify LastSyncDate was NOT updated
            var settings = await db.KsefSettings.FirstAsync();
            settings.LastSyncDate.Should().BeNull();
        }

        [Fact]
        public async Task GetKsefInvoiceXml_ShouldDownloadJustInTime_WhenXmlIsNullInDatabase()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            await PrepareKsefSettingsAsync(db);

            // Seed incoming invoice with null XML (empty string to avoid DB constraint)
            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "1234567890-20260616-111111-ABCDEF",
                SellerName = "Test Seller Ltd",
                SellerNip = "1112223344",
                IssueDate = new DateTime(2026, 6, 16),
                TotalAmount = 123.45m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = string.Empty
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            // Configure Mock Client to return XML when requested JIT
            testKsefClient.XmlToReturn = "<xml>JIT Downloaded Content</xml>";

            var query = new GetKsefInvoiceXmlQuery(incoming.Id);

            // Act
            var result = await mediator.Send(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.RawXml.Should().Be("<xml>JIT Downloaded Content</xml>");
            result.ErrorMessage.Should().BeNull();

            // Verify DB got updated with downloaded XML
            var updated = await db.KsefIncomingInvoices.FindAsync(incoming.Id);
            updated.Should().NotBeNull();
            updated!.RawXml.Should().Be("<xml>JIT Downloaded Content</xml>");
        }

        [Fact]
        public async Task GetKsefInvoicePreview_ShouldFailWithFriendlyError_WhenKsefClientThrowsKsefApiException()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            await PrepareKsefSettingsAsync(db);

            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "1234567890-20260616-111111-ABCDEF",
                SellerName = "Test Seller Ltd",
                SellerNip = "1112223344",
                IssueDate = new DateTime(2026, 6, 16),
                TotalAmount = 123.45m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = string.Empty
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            // Setup download to throw KsefApiException (e.g. 21164 - document not found or session closed)
            testKsefClient.DownloadXmlExceptionToThrow = new KsefApiException(
                "21164",
                "DownloadInvoiceXml",
                "Invoice not found",
                "{}"
            );

            var query = new GetKsefInvoicePreviewQuery(incoming.Id);

            // Act
            var result = await mediator.Send(query, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Nie znaleziono dokumentu o podanym numerze");
        }

        private static string GetValidKsefXml()
        {
            return @"<Faktura>
  <Podmiot1>
    <DanePodmiotu>
      <NIP>1112223344</NIP>
      <Nazwa>Test Seller Ltd</Nazwa>
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
      <NIP>1234567890</NIP>
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
    <P_1>2026-06-16</P_1>
    <P_2>FV/2026/06/100</P_2>
    <P_15>123.45</P_15>
    <FaWiersz>
      <P_7>Service A</P_7>
      <P_8A>1</P_8A>
      <P_9B>123.45</P_9B>
      <P_11>123.45</P_11>
    </FaWiersz>
  </Fa>
</Faktura>";
        }
    }
}
