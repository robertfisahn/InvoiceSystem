using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Ksef
{
    public class KsefSyncBackgroundServiceTests : IntegrationTestBase
    {
        private async Task InvokeSyncAllActiveConfigsAsync(KsefSyncBackgroundService service)
        {
            var method = typeof(KsefSyncBackgroundService)
                .GetMethod("SyncAllActiveConfigsAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method == null)
            {
                throw new InvalidOperationException("Could not find private method SyncAllActiveConfigsAsync in KsefSyncBackgroundService");
            }

            var task = (Task)method.Invoke(service, new object[] { CancellationToken.None })!;
            await task;
        }

        [Fact]
        public async Task Sync_ShouldSyncInvoicesAndSaveRawXml_WhenEnabledAndCredentialsAreValid()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<KsefSyncBackgroundService>>();

            // Seed setting
            var setting = new KsefSetting
            {
                Nip = "1234567890",
                ApiKey = "api-key-xyz",
                IsEnabled = true,
                LastSyncDate = null
            };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();

            // Setup mock client to return 2 invoices
            testKsefClient.ShouldThrow = false;
            testKsefClient.IncomingInvoicesToReturn = new List<KsefIncomingInvoiceDto>
            {
                new KsefIncomingInvoiceDto("1234567890-20260616-111111-AAAAAA", "Seller A", "111", new DateTime(2026, 6, 16), 100m, ""),
                new KsefIncomingInvoiceDto("1234567890-20260616-222222-BBBBBB", "Seller B", "222", new DateTime(2026, 6, 16), 200m, "")
            };
            testKsefClient.XmlToReturn = "<xml>Background Synced Invoice</xml>";

            var service = new KsefSyncBackgroundService(scope.ServiceProvider, logger);

            // Act
            await InvokeSyncAllActiveConfigsAsync(service);

            // Assert
            // 1. Verify invoices imported
            var count = await db.KsefIncomingInvoices.CountAsync();
            count.Should().Be(2);

            var inv1 = await db.KsefIncomingInvoices.FirstOrDefaultAsync(i => i.KsefNumber == "1234567890-20260616-111111-AAAAAA");
            inv1.Should().NotBeNull();
            inv1!.SellerName.Should().Be("Seller A");
            inv1.RawXml.Should().Be("<xml>Background Synced Invoice</xml>");

            // 2. Verify setting LastSyncDate updated
            await db.Entry(setting).ReloadAsync();
            setting.LastSyncDate.Should().NotBeNull();
        }

        [Fact]
        public async Task Sync_ShouldAbortSyncQueueAndNotUpdateLastSyncDate_WhenXmlDownloadThrows429TooManyRequests()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<KsefSyncBackgroundService>>();

            var setting = new KsefSetting
            {
                Nip = "1234567890",
                ApiKey = "api-key-xyz",
                IsEnabled = true,
                LastSyncDate = null
            };
            db.KsefSettings.Add(setting);
            await db.SaveChangesAsync();

            // Setup mock client with 2 invoices
            testKsefClient.ShouldThrow = false;
            testKsefClient.IncomingInvoicesToReturn = new List<KsefIncomingInvoiceDto>
            {
                new KsefIncomingInvoiceDto("1234567890-20260616-111111-AAAAAA", "Seller A", "111", new DateTime(2026, 6, 16), 100m, ""),
                new KsefIncomingInvoiceDto("1234567890-20260616-222222-BBBBBB", "Seller B", "222", new DateTime(2026, 6, 16), 200m, "")
            };
            
            // Set XML download exception to throw 429
            testKsefClient.DownloadXmlExceptionToThrow = new HttpRequestException(
                "Rate limit exceeded",
                null,
                HttpStatusCode.TooManyRequests
            );

            var service = new KsefSyncBackgroundService(scope.ServiceProvider, logger);

            // Act
            await InvokeSyncAllActiveConfigsAsync(service);

            // Assert
            // 1. Verify no invoices imported (since it aborts on the first download error)
            var count = await db.KsefIncomingInvoices.CountAsync();
            count.Should().Be(0);

            // 2. Verify setting LastSyncDate was NOT updated
            var updatedSetting = await db.KsefSettings.FirstAsync();
            updatedSetting.LastSyncDate.Should().BeNull();
        }

        [Fact]
        public async Task Sync_ShouldPollPendingOutgoingInvoices_AndAssignKsefNumbersAndUpo()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var testKsefClient = (TestKsefClient)scope.ServiceProvider.GetRequiredService<IKsefClient>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<KsefSyncBackgroundService>>();

            // Seed setting
            var setting = new KsefSetting
            {
                Nip = "1234567890",
                ApiKey = "api-key-xyz",
                IsEnabled = true
            };
            db.KsefSettings.Add(setting);

            // Seed Contractor
            var contractor = new Contractor
            {
                Name = "Contractor ABC",
                TaxId = "9999999999",
                Address = "Street, City, 00-000"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            // Seed pending outgoing invoice (has KsefTransactionId but no KsefNumber)
            var invoice = new Invoice
            {
                ContractorId = contractor.Id,
                InvoiceNumber = "FV/2026/06/999",
                Date = DateTime.UtcNow,
                Status = InvoiceStatus.Confirmed,
                KsefTransactionId = "online_ref_999:transaction_id_888",
                KsefNumber = null,
                UpoXml = null
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            // Setup mock client status poll
            testKsefClient.ShouldThrow = false;
            testKsefClient.StatusToReturn = "Processed";
            testKsefClient.KsefNumberToReturn = "1234567890-20260616-999999-EEEEEE";

            var service = new KsefSyncBackgroundService(scope.ServiceProvider, logger);

            // Act
            await InvokeSyncAllActiveConfigsAsync(service);

            // Assert
            // Verify outgoing invoice got processed
            await db.Entry(invoice).ReloadAsync();
            invoice.KsefNumber.Should().Be("1234567890-20260616-999999-EEEEEE");
            invoice.UpoXml.Should().Be("<UpoXml/>");
        }
    }
}
