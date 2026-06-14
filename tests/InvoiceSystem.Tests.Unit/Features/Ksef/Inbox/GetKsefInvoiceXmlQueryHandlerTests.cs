using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceXml;
using InvoiceSystem.Web.Infrastructure.Ksef;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Ksef.Inbox.GetKsefInvoiceXml
{
    public class GetKsefInvoiceXmlQueryHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IKsefClient _ksefClientMock;

        public GetKsefInvoiceXmlQueryHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _ksefClientMock = Substitute.For<IKsefClient>();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Invoice_Not_Found()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new GetKsefInvoiceXmlQueryHandler(db, _ksefClientMock);
            var query = new GetKsefInvoiceXmlQuery(999);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Faktura KSeF nie została znaleziona.");
        }

        [Fact]
        public async Task Handle_Should_Return_Xml_When_Available()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-123456",
                SellerName = "Seller",
                SellerNip = "1111111111",
                IssueDate = new DateTime(2026, 6, 14),
                TotalAmount = 150m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = "<xml>content</xml>"
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            var handler = new GetKsefInvoiceXmlQueryHandler(db, _ksefClientMock);
            var query = new GetKsefInvoiceXmlQuery(incoming.Id);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.RawXml.Should().Be("<xml>content</xml>");
        }
    }
}
