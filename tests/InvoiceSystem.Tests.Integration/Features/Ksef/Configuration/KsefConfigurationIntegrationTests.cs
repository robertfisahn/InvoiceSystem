using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Ksef.Configuration;
using InvoiceSystem.Web.Features.Ksef.Configuration.GetKsefConfiguration;
using InvoiceSystem.Web.Features.Ksef.Configuration.SaveKsefConfiguration;
using InvoiceSystem.Web.Features.Ksef.Configuration.TestKsefConnection;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Ksef.Configuration
{
    public class KsefConfigurationIntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task SaveKsefConfiguration_ShouldCreateNewSetting_WhenNoneExist()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure no settings exist initially
            db.KsefSettings.RemoveRange(await db.KsefSettings.ToListAsync());
            await db.SaveChangesAsync();

            var viewModel = new KsefConfigurationViewModel
            {
                Nip = "1234567890",
                ApiKey = "new-api-key",
                IsEnabled = true
            };
            var command = new SaveKsefConfigurationCommand(viewModel);

            // Act
            await mediator.Send(command, CancellationToken.None);

            // Assert
            var saved = await db.KsefSettings.FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Nip.Should().Be("1234567890");
            saved.ApiKey.Should().Be("new-api-key");
            saved.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task SaveKsefConfiguration_ShouldUpdateExistingSetting_WhenAlreadyExist()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Seed initial setting
            var initial = new KsefSetting
            {
                Nip = "9999999999",
                ApiKey = "old-key",
                IsEnabled = false
            };
            db.KsefSettings.Add(initial);
            await db.SaveChangesAsync();

            var viewModel = new KsefConfigurationViewModel
            {
                Nip = "5555555555",
                ApiKey = "updated-key",
                IsEnabled = true
            };
            var command = new SaveKsefConfigurationCommand(viewModel);

            // Act
            await mediator.Send(command, CancellationToken.None);

            // Assert
            var saved = await db.KsefSettings.FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Nip.Should().Be("5555555555");
            saved.ApiKey.Should().Be("updated-key");
            saved.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task TestKsefConnection_ShouldSucceed_WhenCredentialsAreCorrect()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            testKsefClient.ShouldThrow = false;
            testKsefClient.ExceptionToThrow = null;

            var command = new TestKsefConnectionCommand("1234567890", "correct-key");

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("nawiązane pomyślnie");
        }

        [Fact]
        public async Task TestKsefConnection_ShouldFailWithNipError_WhenNipIsInvalid()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            testKsefClient.ExceptionToThrow = new KsefApiException(
                "21405",
                "AuthorisationChallenge",
                "Błędny NIP podmiotu",
                "Validation error for NIP"
            );

            var command = new TestKsefConnectionCommand("invalid-nip", "correct-key");

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Niepoprawny NIP");
        }

        [Fact]
        public async Task TestKsefConnection_ShouldFailWithTokenError_WhenTokenIsInvalid()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            testKsefClient.InitSessionExceptionToThrow = new KsefApiException(
                "21111",
                "InitSession",
                "Invalid API token",
                "Brak uprawnień do nawiązania sesji"
            );

            var command = new TestKsefConnectionCommand("1234567890", "wrong-key");

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Niepoprawny token API lub brak uprawnień");
        }

        [Fact]
        public async Task TestKsefConnection_ShouldFailWithRateLimitError_WhenKsefReturns429()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();

            testKsefClient.ExceptionToThrow = new HttpRequestException(
                "Rate limit exceeded",
                null,
                HttpStatusCode.TooManyRequests
            );

            var command = new TestKsefConnectionCommand("1234567890", "correct-key");

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Przekroczono limit żądań");
        }
    }
}
