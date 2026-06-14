using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInbox;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Ksef.Inbox.GetKsefInbox
{
    public class GetKsefInboxQueryHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public GetKsefInboxQueryHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_EmptyList_And_Flags_When_DatabaseIsEmpty()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new GetKsefInboxQueryHandler(db);
            var query = new GetKsefInboxQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Invoices.Should().BeEmpty();
            result.KsefEnabled.Should().BeFalse();
            result.KsefConfigured.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Should_Return_Invoices_OrderedByIssueDateDescending()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            
            var inv1 = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-1",
                SellerName = "Seller 1",
                SellerNip = "111",
                IssueDate = new DateTime(2026, 6, 1),
                TotalAmount = 100m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = "<xml/>"
            };

            var inv2 = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-2",
                SellerName = "Seller 2",
                SellerNip = "222",
                IssueDate = new DateTime(2026, 6, 10),
                TotalAmount = 200m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = "<xml/>"
            };

            db.KsefIncomingInvoices.AddRange(inv1, inv2);

            var setting = new KsefSetting
            {
                Nip = "123",
                ApiKey = "key",
                IsEnabled = true
            };
            db.KsefSettings.Add(setting);
            
            await db.SaveChangesAsync();

            var handler = new GetKsefInboxQueryHandler(db);
            var query = new GetKsefInboxQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.KsefEnabled.Should().BeTrue();
            result.KsefConfigured.Should().BeTrue();
            result.Invoices.Should().HaveCount(2);
            result.Invoices[0].KsefNumber.Should().Be("KSEF-2");
            result.Invoices[1].KsefNumber.Should().Be("KSEF-1");
        }
    }
}
