using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using InvoiceSystem.Web.Modules.Ksef.Features.Inbox.SyncKsefInbox;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Ksef.Features.Inbox.SyncKsefInbox
{
    public class SyncKsefInboxCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IKsefClient _ksefClient;
        private readonly ILogger<SyncKsefInboxCommandHandler> _logger;
        private readonly IKsefSyncLock _syncLock;

        public SyncKsefInboxCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _ksefClient = Substitute.For<IKsefClient>();
            _logger = Substitute.For<ILogger<SyncKsefInboxCommandHandler>>();
            _syncLock = Substitute.For<IKsefSyncLock>();
            _syncLock.TryAcquireAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Settings_Are_Missing()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var syncService = new KsefSyncService(_syncLock, db, _ksefClient, Substitute.For<ILogger<KsefSyncService>>());
            var handler = new SyncKsefInboxCommandHandler(db, syncService, _logger);
            var command = new SyncKsefInboxCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Integracja KSeF nie jest poprawnie skonfigurowana.");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_API_InitSession_Throws_Exception()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var setting = new KsefSetting { Nip = "1234567890", ApiKey = "KEY-123" };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();

            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception("KSeF connection failed"));

            var syncService = new KsefSyncService(_syncLock, db, _ksefClient, Substitute.For<ILogger<KsefSyncService>>());
            var handler = new SyncKsefInboxCommandHandler(db, syncService, _logger);
            var command = new SyncKsefInboxCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("KSeF connection failed");
        }

        [Fact]
        public async Task Handle_Should_Sync_New_Invoices_And_Save_Xml_Content()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var setting = new KsefSetting { Nip = "1234567890", ApiKey = "KEY-123", LastSyncDate = null };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();

            const string sessionToken = "SESSION-REF";
            
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .Returns(sessionToken);

            var dtoList = new List<KsefIncomingInvoiceDto>
            {
                new KsefIncomingInvoiceDto("NIP-123", "Seller A", "9876543210", new DateTime(2026, 6, 1), 150.00m, ""),
                new KsefIncomingInvoiceDto("NIP-456", "Seller B", "8888888888", new DateTime(2026, 6, 2), 250.00m, "")
            };

            _ksefClient.SyncInvoicesAsync(sessionToken, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(dtoList);

            _ksefClient.DownloadInvoiceXmlAsync(sessionToken, "NIP-123", Arg.Any<CancellationToken>())
                .Returns("<xml>Content 123</xml>");
            _ksefClient.DownloadInvoiceXmlAsync(sessionToken, "NIP-456", Arg.Any<CancellationToken>())
                .Returns("<xml>Content 456</xml>");

            var syncService = new KsefSyncService(_syncLock, db, _ksefClient, Substitute.For<ILogger<KsefSyncService>>());
            var handler = new SyncKsefInboxCommandHandler(db, syncService, _logger);
            var command = new SyncKsefInboxCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.NewInvoicesCount.Should().Be(2);
            result.ErrorMessage.Should().BeNull();

            // Verify invoices were saved in database
            var saved1 = await db.KsefIncomingInvoices.FirstOrDefaultAsync(i => i.KsefNumber == "NIP-123");
            saved1.Should().NotBeNull();
            saved1!.SellerName.Should().Be("Seller A");
            saved1.SellerNip.Should().Be("9876543210");
            saved1.TotalAmount.Should().Be(150.00m);
            saved1.RawXml.Should().Be("<xml>Content 123</xml>");
            saved1.ImportStatus.Should().Be(KsefImportStatus.Pending);

            var saved2 = await db.KsefIncomingInvoices.FirstOrDefaultAsync(i => i.KsefNumber == "NIP-456");
            saved2.Should().NotBeNull();

            // Verify settings LastSyncDate got updated since no errors occurred
            var updatedSetting = await db.KsefSettings.FirstAsync();
            updatedSetting.LastSyncDate.Should().NotBeNull();

            // Verify session was closed
            await _ksefClient.Received(1).CloseSessionAsync(sessionToken, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_Should_Skip_Existing_Invoices_And_Abort_On_TooManyRequests_Error()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var setting = new KsefSetting { Nip = "1234567890", ApiKey = "KEY-123", LastSyncDate = null };
            db.KsefSettings.Add(setting);

            // Pre-seed an existing invoice to verify skip logic
            var existing = new KsefIncomingInvoice
            {
                KsefNumber = "EXISTS-1",
                SellerName = "Seller Exist",
                SellerNip = "1111",
                IssueDate = DateTime.Today,
                TotalAmount = 100m,
                RawXml = "<xml></xml>",
                ImportStatus = KsefImportStatus.Pending
            };
            db.KsefIncomingInvoices.Add(existing);
            await db.SaveChangesAsync();

            const string sessionToken = "SESSION-REF";
            
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .Returns(sessionToken);

            var dtoList = new List<KsefIncomingInvoiceDto>
            {
                new KsefIncomingInvoiceDto("EXISTS-1", "Seller Exist", "1111", DateTime.Today, 100m, ""), // Exists (will be skipped)
                new KsefIncomingInvoiceDto("NEW-1", "Seller New", "2222", DateTime.Today, 200m, ""),      // Will trigger 429
                new KsefIncomingInvoiceDto("NEW-2", "Seller New 2", "3333", DateTime.Today, 300m, "")     // Will be aborted (never requested)
            };

            _ksefClient.SyncInvoicesAsync(sessionToken, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(dtoList);

            // Setup NEW-1 XML download to throw HttpRequestException with 429
            var httpEx429 = new HttpRequestException("Too many requests", null, HttpStatusCode.TooManyRequests);
            _ksefClient.DownloadInvoiceXmlAsync(sessionToken, "NEW-1", Arg.Any<CancellationToken>())
                .ThrowsAsync(httpEx429);

            var syncService = new KsefSyncService(_syncLock, db, _ksefClient, Substitute.For<ILogger<KsefSyncService>>());
            var handler = new SyncKsefInboxCommandHandler(db, syncService, _logger);
            var command = new SyncKsefInboxCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.NewInvoicesCount.Should().Be(0);
            result.ErrorMessage.Should().Contain("limit zapytań KSeF (429)");

            // Verify XML download for NEW-2 was never called
            await _ksefClient.DidNotReceive().DownloadInvoiceXmlAsync(sessionToken, "NEW-2", Arg.Any<CancellationToken>());

            // Verify settings LastSyncDate was NOT updated due to errors
            var updatedSetting = await db.KsefSettings.FirstAsync();
            updatedSetting.LastSyncDate.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Continue_Syncing_On_Generic_Exception_And_Report_Success_With_Errors()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var today = DateTime.Today;
            var setting = new KsefSetting { Nip = "1234567890", ApiKey = "KEY-123", LastSyncDate = null };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();

            const string sessionToken = "SESSION-REF";
            
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .Returns(sessionToken);

            var dtoList = new List<KsefIncomingInvoiceDto>
            {
                new KsefIncomingInvoiceDto("FAIL-1", "Seller F", "1111", today.AddDays(-1), 100m, ""), 
                new KsefIncomingInvoiceDto("OK-1", "Seller OK", "2222", today, 200m, "")
            };

            _ksefClient.SyncInvoicesAsync(sessionToken, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(dtoList);

            // FAIL-1 fails with generic connection error
            _ksefClient.DownloadInvoiceXmlAsync(sessionToken, "FAIL-1", Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception("Generic API failure"));

            // OK-1 succeeds
            _ksefClient.DownloadInvoiceXmlAsync(sessionToken, "OK-1", Arg.Any<CancellationToken>())
                .Returns("<xml>OK</xml>");

            var syncService = new KsefSyncService(_syncLock, db, _ksefClient, Substitute.For<ILogger<KsefSyncService>>());
            var handler = new SyncKsefInboxCommandHandler(db, syncService, _logger);
            var command = new SyncKsefInboxCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.NewInvoicesCount.Should().Be(1);

            // Under incremental watermarking, OK-1 succeeds so LastSyncDate should update to today
            var updatedSetting = await db.KsefSettings.FirstAsync();
            updatedSetting.LastSyncDate.Should().Be(today);

            // Verify OK-1 got saved
            var saved = await db.KsefIncomingInvoices.FirstOrDefaultAsync(i => i.KsefNumber == "OK-1");
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_InitSession_Throws_KsefApiException()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var setting = new KsefSetting { Nip = "1234567890", ApiKey = "KEY-123" };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();

            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            var mockKsefException = new KsefApiException(
                serviceCode: "21111",
                serviceName: "InitSession",
                serviceCtx: "Błędne uwierzytelnienie - niepoprawny token.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21111\", \"serviceCtx\": \"Błędne uwierzytelnienie - niepoprawny token.\", \"serviceName\": \"InitSession\"}}"
            );
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockKsefException);

            var syncService = new KsefSyncService(_syncLock, db, _ksefClient, Substitute.For<ILogger<KsefSyncService>>());
            var handler = new SyncKsefInboxCommandHandler(db, syncService, _logger);
            var command = new SyncKsefInboxCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd autoryzacji KSeF: Niepoprawny token API lub brak uprawnień");
        }
    }
}
