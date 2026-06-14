using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Ksef.Configuration.TestKsefConnection;
using InvoiceSystem.Web.Infrastructure.Ksef;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Ksef.Configuration.TestKsefConnection
{
    public class TestKsefConnectionCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IKsefClient _ksefClient;

        public TestKsefConnectionCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _ksefClient = Substitute.For<IKsefClient>();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_ApiKey_Is_Missing_And_No_Settings_In_Db()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Brak zapisanej konfiguracji KSeF. Wprowadź NIP oraz Token przed uruchomieniem testu.");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Nip_Is_Missing()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Wprowadź NIP firmy.");
        }

        [Fact]
        public async Task Handle_Should_Fall_Back_To_Db_Settings_When_ApiKey_Is_Missing_In_Command()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var setting = new KsefSetting { Nip = "1234567890", ApiKey = "DB-KEY-123" };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();

            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));
            _ksefClient.InitSessionAsync("1234567890", "DB-KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .Returns("SESSION-TOKEN");

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("9999999999", ""); // Nip in command is overwritten if ApiKey is empty

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Połączenie z Sandboxem KSeF nawiązane pomyślnie!");
            
            await _ksefClient.Received(1).InitSessionAsync("1234567890", "DB-KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>());
            await _ksefClient.Received(1).CloseSessionAsync("SESSION-TOKEN", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_Should_Use_Command_Params_When_ApiKey_Is_Provided()
        {
            // Arrange
            using var db = _fixture.CreateContext();

            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .Returns("SESSION-TOKEN");

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Połączenie z Sandboxem KSeF nawiązane pomyślnie!");

            await _ksefClient.Received(1).InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>());
            await _ksefClient.Received(1).CloseSessionAsync("SESSION-TOKEN", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Exception_Occurs_During_Call()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception("KSeF API is offline"));

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Błąd połączenia: KSeF API is offline");
        }
    }
}
