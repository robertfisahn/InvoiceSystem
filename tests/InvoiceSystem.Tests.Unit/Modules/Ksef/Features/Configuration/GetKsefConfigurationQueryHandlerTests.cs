using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using InvoiceSystem.Web.Modules.Ksef.Features.Configuration;
using InvoiceSystem.Web.Modules.Ksef.Features.Configuration.GetKsefConfiguration;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Ksef.Features.Configuration
{
    public class GetKsefConfigurationQueryHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public GetKsefConfigurationQueryHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_ViewModel_When_No_Settings_Exist()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new GetKsefConfigurationQueryHandler(db);
            var query = new GetKsefConfigurationQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Nip.Should().BeEmpty();
            result.ApiKey.Should().BeEmpty();
            result.IsEnabled.Should().BeFalse();
            result.ActiveSessionToken.Should().BeNull();
            result.SessionExpiresAt.Should().BeNull();
            result.LastSyncDate.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Return_Mapped_ViewModel_When_Settings_Exist()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var setting = new KsefSetting
            {
                Nip = "1234567890",
                ApiKey = "some_api_key",
                IsEnabled = true,
                ActiveSessionToken = "session_token",
                SessionExpiresAt = DateTime.UtcNow.AddHours(2),
                LastSyncDate = DateTime.UtcNow.AddDays(-1)
            };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();

            var handler = new GetKsefConfigurationQueryHandler(db);
            var query = new GetKsefConfigurationQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Nip.Should().Be("1234567890");
            result.ApiKey.Should().Be("some_api_key");
            result.IsEnabled.Should().BeTrue();
            result.ActiveSessionToken.Should().Be("session_token");
            result.SessionExpiresAt.Should().BeCloseTo(setting.SessionExpiresAt!.Value, TimeSpan.FromSeconds(1));
            result.LastSyncDate.Should().BeCloseTo(setting.LastSyncDate!.Value, TimeSpan.FromSeconds(1));
        }
    }
}
