using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Ksef.Domain;
using InvoiceSystem.Web.Modules.Ksef.Features.Configuration;
using InvoiceSystem.Web.Modules.Ksef.Features.Configuration.SaveKsefConfiguration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Ksef.Features.Configuration
{
    public class SaveKsefConfigurationCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public SaveKsefConfigurationCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Create_New_Setting_When_None_Exists()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new SaveKsefConfigurationCommandHandler(db);
            var model = new KsefConfigurationViewModel
            {
                Nip = "1234567890",
                ApiKey = "new_api_key",
                IsEnabled = true
            };
            var command = new SaveKsefConfigurationCommand(model);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            using var verifyDb = _fixture.CreateContext();
            var settings = await verifyDb.KsefSettings.ToListAsync();
            settings.Should().ContainSingle();
            settings[0].Nip.Should().Be("1234567890");
            settings[0].ApiKey.Should().Be("new_api_key");
            settings[0].IsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Update_Existing_Setting_When_It_Exists()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var existingSetting = new KsefSetting
            {
                Nip = "9999999999",
                ApiKey = "old_api_key",
                IsEnabled = false
            };
            db.KsefSettings.Add(existingSetting);
            await db.SaveChangesAsync();

            var handler = new SaveKsefConfigurationCommandHandler(db);
            var model = new KsefConfigurationViewModel
            {
                Nip = "1234567890",
                ApiKey = "updated_api_key",
                IsEnabled = true
            };
            var command = new SaveKsefConfigurationCommand(model);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            using var verifyDb = _fixture.CreateContext();
            var settings = await verifyDb.KsefSettings.ToListAsync();
            settings.Should().ContainSingle();
            settings[0].Nip.Should().Be("1234567890");
            settings[0].ApiKey.Should().Be("updated_api_key");
            settings[0].IsEnabled.Should().BeTrue();
        }
    }
}
