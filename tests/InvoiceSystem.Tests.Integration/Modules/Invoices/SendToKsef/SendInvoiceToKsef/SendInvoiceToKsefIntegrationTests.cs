using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Modules.Auth.Domain;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Invoices.Features.SendToKsef.SendInvoiceToKsef;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Modules.Invoices.SendToKsef.SendInvoiceToKsef
{
    public class SendInvoiceToKsefIntegrationTests : IntegrationTestBase
    {
        private async Task<(Contractor, Invoice)> PrepareConfirmedInvoiceAsync(AppDbContext db)
        {
            var contractor = new Contractor
            {
                Name = "KSeF Partner Sp. z o.o.",
                TaxId = "5250000123",
                Address = "Jasna 10, Warszawa"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                ContractorId = contractor.Id,
                InvoiceNumber = "INV/2026/100",
                Date = DateTime.UtcNow,
                Status = InvoiceStatus.Confirmed, // Must be confirmed to send to KSeF
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Name = "Goods A", Quantity = 2, UnitPrice = 100.00m, TotalPrice = 200.00m }
                }
            };
            db.Invoices.Add(invoice);

            var ksefSetting = new KsefSetting
            {
                Nip = "5250000123",
                ApiKey = "dummy-ksef-token-123456789"
            };
            db.KsefSettings.Add(ksefSetting);

            await db.SaveChangesAsync();
            return (contractor, invoice);
        }

        [Fact]
        public async Task SendToKsef_ShouldSucceedAndPopulateKsefNumber_WhenKsefClientSucceeds()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            // Configure Mock behaviors
            testKsefClient.ShouldThrow = false;
            testKsefClient.ExceptionToThrow = null;
            testKsefClient.StatusToReturn = "Processed";
            testKsefClient.KsefNumberToReturn = "5250000123-20260615-999999-FFFFFF";

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.KsefNumber.Should().Be("5250000123-20260615-999999-FFFFFF");

            // Verify database update
            var updatedInvoice = await dbContext.Invoices.FindAsync(invoice.Id);
            updatedInvoice.Should().NotBeNull();
            updatedInvoice!.KsefNumber.Should().Be("5250000123-20260615-999999-FFFFFF");
            updatedInvoice.KsefTransactionId.Should().Contain("transaction_id_888");
            updatedInvoice.UpoXml.Should().Be("<UpoXml/>");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnErrorMessage_WhenKsefClientThrowsException()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            // Force Mock client to fail
            testKsefClient.ShouldThrow = true;
            testKsefClient.ExceptionToThrow = null;
            testKsefClient.ErrorMessage = "External Government KSeF API is down (503 Service Unavailable).";

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.KsefNumber.Should().BeNull();
            result.ErrorMessage.Should().Be("External Government KSeF API is down (503 Service Unavailable).");

            // Verify database has NOT been updated with transaction IDs
            var updatedInvoice = await dbContext.Invoices.FindAsync(invoice.Id);
            updatedInvoice.Should().NotBeNull();
            updatedInvoice!.KsefNumber.Should().BeNullOrEmpty();
            updatedInvoice.KsefTransactionId.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnDetailedErrorMessage_WhenKsefClientThrowsKsefApiException()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            // Setup a mock exception mimicking real KSeF NIP mismatch validation error
            var mockKsefException = new KsefApiException(
                serviceCode: "21405",
                serviceName: "SendInvoice",
                serviceCtx: "Wartość pola Nip sprzedawcy jest niezgodna z numerem Nip podanym w kontekście sesji.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21405\", \"serviceCtx\": \"...\", \"serviceName\": \"SendInvoice\"}}"
            );
            testKsefClient.ExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.KsefNumber.Should().BeNull();
            result.ErrorMessage.Should().Be("Błąd KSeF: NIP sprzedawcy na fakturze jest niezgodny z NIP-em zalogowanej sesji. Szczegóły: Wartość pola Nip sprzedawcy jest niezgodna z numerem Nip podanym w kontekście sesji.");

            // Verify database has NOT been updated
            var updatedInvoice = await dbContext.Invoices.FindAsync(invoice.Id);
            updatedInvoice.Should().NotBeNull();
            updatedInvoice!.KsefNumber.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenChallengeReturns400BadRequest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockKsefException = new KsefApiException(
                serviceCode: "21405",
                serviceName: "AuthorisationChallenge",
                serviceCtx: "Niepoprawny format identyfikatora NIP.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21405\", \"serviceCtx\": \"Niepoprawny format identyfikatora NIP.\", \"serviceName\": \"AuthorisationChallenge\"}}"
            );
            testKsefClient.ExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd walidacji KSeF: Niepoprawny NIP");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenChallengeReturns429TooManyRequests()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Too Many Requests", 
                null, 
                System.Net.HttpStatusCode.TooManyRequests
            );
            testKsefClient.ExceptionToThrow = mockHttpException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Przekroczono limit żądań do KSeF (429)");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenInitSessionReturns400BadRequest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockKsefException = new KsefApiException(
                serviceCode: "21111",
                serviceName: "InitSession",
                serviceCtx: "Brak uprawnień do wykonania operacji.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21111\", \"serviceCtx\": \"Brak uprawnień do wykonania operacji.\", \"serviceName\": \"InitSession\"}}"
            );
            testKsefClient.InitSessionExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd autoryzacji KSeF: Niepoprawny token API lub brak uprawnień");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenInitSessionReturns429TooManyRequests()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Too Many Requests", 
                null, 
                System.Net.HttpStatusCode.TooManyRequests
            );
            testKsefClient.InitSessionExceptionToThrow = mockHttpException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Przekroczono limit żądań do KSeF (429)");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenCheckSessionStatusReturns400BadRequest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockKsefException = new KsefApiException(
                serviceCode: "21304",
                serviceName: "CheckSessionStatus",
                serviceCtx: "Nieprawidłowy numer referencyjny.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21304\", \"serviceCtx\": \"Nieprawidłowy numer referencyjny.\", \"serviceName\": \"CheckSessionStatus\"}}"
            );
            testKsefClient.InitSessionExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd KSeF: Nieprawidłowy numer referencyjny sesji");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenCheckSessionStatusReturns410Gone()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Gone", 
                null, 
                System.Net.HttpStatusCode.Gone
            );
            testKsefClient.InitSessionExceptionToThrow = mockHttpException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Sesja KSeF wygasła (410 Gone)");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenRedeemTokenReturns400BadRequest_21301()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockKsefException = new KsefApiException(
                serviceCode: "21301",
                serviceName: "RedeemToken",
                serviceCtx: "Brak uprawnień do tokenu sesyjnego.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21301\", \"serviceCtx\": \"Brak uprawnień do tokenu sesyjnego.\", \"serviceName\": \"RedeemToken\"}}"
            );
            testKsefClient.InitSessionExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd autoryzacji tokenu KSeF: Niepoprawny token sesji lub brak uprawnień");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenSendInvoiceReturns400BadRequest_21405()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockKsefException = new KsefApiException(
                serviceCode: "21405",
                serviceName: "SendInvoice",
                serviceCtx: "NIP sprzedawcy niezgodny z zalogowaną sesją.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21405\", \"serviceCtx\": \"NIP sprzedawcy niezgodny z zalogowaną sesją.\", \"serviceName\": \"SendInvoice\"}}"
            );
            testKsefClient.SendInvoiceExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd KSeF: NIP sprzedawcy na fakturze jest niezgodny z NIP-em zalogowanej sesji");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenCheckInvoiceStatusReturns400BadRequest_21304()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockKsefException = new KsefApiException(
                serviceCode: "21304",
                serviceName: "CheckInvoiceStatus",
                serviceCtx: "Niepoprawny identyfikator transakcji.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21304\", \"serviceCtx\": \"Niepoprawny identyfikator transakcji.\", \"serviceName\": \"CheckInvoiceStatus\"}}"
            );
            testKsefClient.GetInvoiceStatusExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd KSeF: Nieprawidłowy identyfikator transakcji podczas sprawdzania statusu wysyłki");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenSendInvoiceReturns429TooManyRequests()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Too Many Requests", 
                null, 
                System.Net.HttpStatusCode.TooManyRequests
            );
            testKsefClient.SendInvoiceExceptionToThrow = mockHttpException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Przekroczono limit żądań do KSeF (429)");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenCheckInvoiceStatusReturns429TooManyRequests()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Too Many Requests", 
                null, 
                System.Net.HttpStatusCode.TooManyRequests
            );
            testKsefClient.GetInvoiceStatusExceptionToThrow = mockHttpException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Przekroczono limit żądań do KSeF (429)");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenDownloadUpoReturns400BadRequest_21304()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockKsefException = new KsefApiException(
                serviceCode: "21304",
                serviceName: "DownloadUpo",
                serviceCtx: "Nieprawidłowy numer KSeF.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21304\", \"serviceCtx\": \"Nieprawidłowy numer KSeF.\", \"serviceName\": \"DownloadUpo\"}}"
            );
            testKsefClient.DownloadUpoExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd KSeF: Nieprawidłowy numer KSeF podczas pobierania UPO");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenDownloadUpoReturns429TooManyRequests()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockHttpException = new System.Net.Http.HttpRequestException(
                "Too Many Requests", 
                null, 
                System.Net.HttpStatusCode.TooManyRequests
            );
            testKsefClient.DownloadUpoExceptionToThrow = mockHttpException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Przekroczono limit żądań do KSeF (429)");
        }

        [Fact]
        public async Task SendToKsef_ShouldFailAndReturnFriendlyErrorMessage_WhenCloseSessionReturns400BadRequest()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            var mockKsefException = new KsefApiException(
                serviceCode: "21304",
                serviceName: "CloseSession",
                serviceCtx: "Nieprawidłowy numer sesji.",
                rawResponse: "{\"exception\": {\"serviceCode\": \"21304\", \"serviceCtx\": \"Nieprawidłowy numer sesji.\", \"serviceName\": \"CloseSession\"}}"
            );
            testKsefClient.CloseSessionExceptionToThrow = mockKsefException;

            var (_, invoice) = await PrepareConfirmedInvoiceAsync(dbContext);

            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Błąd zamykania sesji KSeF");
        }
    }
}
