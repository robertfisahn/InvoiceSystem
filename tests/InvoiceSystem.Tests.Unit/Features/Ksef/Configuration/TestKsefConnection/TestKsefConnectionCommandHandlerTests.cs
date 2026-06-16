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

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_Challenge_Throws_KsefApiException_With_Invalid_Nip()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var mockKsefException = new KsefApiException(
                serviceCode: "21405",
                serviceName: "AuthorisationChallenge",
                serviceCtx: "Niepoprawny format identyfikatora NIP.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21405\", \"serviceCtx\": \"Niepoprawny format identyfikatora NIP.\", \"serviceName\": \"AuthorisationChallenge\"}}"
            );
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockKsefException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Błąd walidacji KSeF: Niepoprawny NIP");
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_Challenge_Throws_429_TooManyRequests()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Too Many Requests", 
                null, 
                System.Net.HttpStatusCode.TooManyRequests
            );
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockHttpException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Przekroczono limit żądań do KSeF (429)");
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_InitSession_Throws_KsefApiException_With_Invalid_Token()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            var mockKsefException = new KsefApiException(
                serviceCode: "21111",
                serviceName: "InitSession",
                serviceCtx: "Brak uprawnień do wykonania operacji.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21111\", \"serviceCtx\": \"Brak uprawnień do wykonania operacji.\", \"serviceName\": \"InitSession\"}}"
            );
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockKsefException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Błąd autoryzacji KSeF: Niepoprawny token API lub brak uprawnień");
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_InitSession_Throws_429_TooManyRequests()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Too Many Requests", 
                null, 
                System.Net.HttpStatusCode.TooManyRequests
            );
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockHttpException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Przekroczono limit żądań do KSeF (429)");
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_CheckSessionStatus_Throws_KsefApiException_With_Invalid_ReferenceNumber()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            var mockKsefException = new KsefApiException(
                serviceCode: "21304",
                serviceName: "CheckSessionStatus",
                serviceCtx: "Nieprawidłowy numer referencyjny.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21304\", \"serviceCtx\": \"Nieprawidłowy numer referencyjny.\", \"serviceName\": \"CheckSessionStatus\"}}"
            );
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockKsefException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Błąd KSeF: Nieprawidłowy numer referencyjny sesji");
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_CheckSessionStatus_Throws_410_Gone()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Gone", 
                null, 
                System.Net.HttpStatusCode.Gone
            );
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockHttpException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Sesja KSeF wygasła (410 Gone)");
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_RedeemToken_Throws_KsefApiException_With_Invalid_SessionToken()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            var mockKsefException = new KsefApiException(
                serviceCode: "21301",
                serviceName: "RedeemToken",
                serviceCtx: "Brak uprawnień do tokenu sesyjnego.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21301\", \"serviceCtx\": \"Brak uprawnień do tokenu sesyjnego.\", \"serviceName\": \"RedeemToken\"}}"
            );
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockKsefException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Błąd autoryzacji tokenu KSeF: Niepoprawny token sesji lub brak uprawnień");
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_OpenSession_Throws_KsefApiException_With_AuthError()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));

            var mockKsefException = new KsefApiException(
                serviceCode: "21470",
                serviceName: "OpenSession",
                serviceCtx: "Błąd uwierzytelniania sesji.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21470\", \"serviceCtx\": \"Błąd uwierzytelniania sesji.\", \"serviceName\": \"OpenSession\"}}"
            );
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockKsefException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Błąd otwarcia sesji KSeF: Błąd uwierzytelniania");
        }

        [Fact]
        public async Task Handle_Should_Return_Friendly_Error_When_CloseSession_Throws_KsefApiException()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "TIMESTAMP"));
            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "TIMESTAMP", Arg.Any<CancellationToken>())
                .Returns("mock_token|ref|aes|iv|online_ref_999");

            var mockKsefException = new KsefApiException(
                serviceCode: "21304",
                serviceName: "CloseSession",
                serviceCtx: "Nieprawidłowy numer sesji.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21304\", \"serviceCtx\": \"Nieprawidłowy numer sesji.\", \"serviceName\": \"CloseSession\"}}"
            );
            _ksefClient.CloseSessionAsync("mock_token|ref|aes|iv|online_ref_999", Arg.Any<CancellationToken>())
                .ThrowsAsync(mockKsefException);

            var handler = new TestKsefConnectionCommandHandler(db, _ksefClient);
            var command = new TestKsefConnectionCommand("1234567890", "KEY-123");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Błąd zamykania sesji KSeF");
        }
    }
}
