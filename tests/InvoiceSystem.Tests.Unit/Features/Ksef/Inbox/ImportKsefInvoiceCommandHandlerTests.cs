using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Ksef.Inbox.ImportKsefInvoice;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Ksef.Inbox.ImportKsefInvoice
{
    public class ImportKsefInvoiceCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public ImportKsefInvoiceCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
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
            var handler = new ImportKsefInvoiceCommandHandler(db);
            var command = new ImportKsefInvoiceCommand(999);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Faktura KSeF nie została znaleziona.");
        }

        [Theory]
        [InlineData(KsefImportStatus.Imported)]
        [InlineData(KsefImportStatus.Ignored)]
        public async Task Handle_Should_Return_Error_When_Status_Is_Not_Pending(KsefImportStatus nonPendingStatus)
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-1",
                SellerName = "Seller",
                SellerNip = "111",
                IssueDate = DateTime.Today,
                TotalAmount = 100m,
                ImportStatus = nonPendingStatus,
                RawXml = "<xml/>"
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            var handler = new ImportKsefInvoiceCommandHandler(db);
            var command = new ImportKsefInvoiceCommand(incoming.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Faktura została już zaimportowana lub zignorowana.");
        }

        [Fact]
        public async Task Handle_Should_Create_New_Contractor_And_Invoice_When_Contractor_Does_Not_Exist()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-12345",
                SellerName = "Seller Name",
                SellerNip = "1111111111",
                IssueDate = new DateTime(2026, 6, 14),
                TotalAmount = 150m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = GetValidKsefXml()
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            var handler = new ImportKsefInvoiceCommandHandler(db);
            var command = new ImportKsefInvoiceCommand(incoming.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.InvoiceNumber.Should().Be("FV/2026/06/1");
            result.SellerName.Should().Be("Seller Name");

            // Verify Database state
            using var verifyDb = _fixture.CreateContext();
            var savedIncoming = await verifyDb.KsefIncomingInvoices.FindAsync(incoming.Id);
            savedIncoming.Should().NotBeNull();
            savedIncoming!.ImportStatus.Should().Be(KsefImportStatus.Imported);
            savedIncoming.ImportedInvoiceId.Should().NotBeNull();

            // Verify Contractor was created
            var contractor = await verifyDb.Contractors.FirstOrDefaultAsync(c => c.TaxId == "1111111111");
            contractor.Should().NotBeNull();
            contractor!.Name.Should().Be("Seller Name");
            contractor.Address.Should().Be("Street Seller 10, 11-111 City Seller");

            // Verify Invoice was created
            var invoice = await verifyDb.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == savedIncoming.ImportedInvoiceId);
            invoice.Should().NotBeNull();
            invoice!.InvoiceNumber.Should().Be("FV/2026/06/1");
            invoice.ContractorId.Should().Be(contractor.Id);
            invoice.Status.Should().Be(InvoiceStatus.Confirmed);
            invoice.KsefNumber.Should().Be("KSEF-12345");
            invoice.KsefSentAt.Should().Be(incoming.IssueDate);

            // Verify items
            invoice.Items.Should().ContainSingle();
            var firstItem = System.Linq.Enumerable.First(invoice.Items);
            firstItem.Name.Should().Be("Service A");
            firstItem.Quantity.Should().Be(1);
            firstItem.UnitPrice.Should().Be(150m);
        }

        [Fact]
        public async Task Handle_Should_Use_Existing_Contractor_When_Contractor_Already_Exists()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            
            // Existing contractor with NIP 1111111111
            var existingContractor = new Contractor
            {
                Name = "Existing Seller Co",
                TaxId = "1111111111",
                Address = "Existing Address"
            };
            db.Contractors.Add(existingContractor);

            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-12345",
                SellerName = "Seller Name",
                SellerNip = "1111111111",
                IssueDate = new DateTime(2026, 6, 14),
                TotalAmount = 150m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = GetValidKsefXml()
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            var handler = new ImportKsefInvoiceCommandHandler(db);
            var command = new ImportKsefInvoiceCommand(incoming.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // Verify Database state
            using var verifyDb = _fixture.CreateContext();
            var savedIncoming = await verifyDb.KsefIncomingInvoices.FindAsync(incoming.Id);
            savedIncoming.Should().NotBeNull();
            
            // Verify Invoice links to existing contractor
            var invoice = await verifyDb.Invoices.FindAsync(savedIncoming!.ImportedInvoiceId);
            invoice.Should().NotBeNull();
            invoice!.ContractorId.Should().Be(existingContractor.Id);

            // Make sure NO new contractor was created for that NIP
            var contractorsCount = await verifyDb.Contractors.CountAsync(c => c.TaxId == "1111111111");
            contractorsCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_Should_Rollback_And_Return_Failure_When_Xml_Is_Invalid()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var incoming = new KsefIncomingInvoice
            {
                KsefNumber = "KSEF-12345",
                SellerName = "Seller Name",
                SellerNip = "1111111111",
                IssueDate = new DateTime(2026, 6, 14),
                TotalAmount = 150m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = "invalid_xml"
            };
            db.KsefIncomingInvoices.Add(incoming);
            await db.SaveChangesAsync();

            var handler = new ImportKsefInvoiceCommandHandler(db);
            var command = new ImportKsefInvoiceCommand(incoming.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();

            // Verify state is unchanged
            using var verifyDb = _fixture.CreateContext();
            var savedIncoming = await verifyDb.KsefIncomingInvoices.FindAsync(incoming.Id);
            savedIncoming!.ImportStatus.Should().Be(KsefImportStatus.Pending);
            savedIncoming.ImportedInvoiceId.Should().BeNull();
            
            var invoicesCount = await verifyDb.Invoices.CountAsync();
            invoicesCount.Should().Be(0);
        }

        private static string GetValidKsefXml()
        {
            return @"<Faktura>
  <Podmiot1>
    <DanePodmiotu>
      <NIP>1111111111</NIP>
      <Nazwa>Seller Name</Nazwa>
    </DanePodmiotu>
    <Adres>
      <AdresPol>
        <Ulica>Street Seller</Ulica>
        <NrDomu>10</NrDomu>
        <KodPocztowy>11-111</KodPocztowy>
        <Miejscowosc>City Seller</Miejscowosc>
      </AdresPol>
    </Adres>
  </Podmiot1>
  <Podmiot2>
    <DanePodmiotu>
      <NIP>2222222222</NIP>
      <Nazwa>Buyer Name</Nazwa>
    </DanePodmiotu>
    <Adres>
      <AdresPol>
        <Ulica>Street Buyer</Ulica>
        <NrDomu>20</NrDomu>
        <KodPocztowy>22-222</KodPocztowy>
        <Miejscowosc>City Buyer</Miejscowosc>
      </AdresPol>
    </Adres>
  </Podmiot2>
  <Fa>
    <P_1>2026-06-14</P_1>
    <P_2>FV/2026/06/1</P_2>
    <P_15>150.00</P_15>
    <FaWiersz>
      <P_7>Service A</P_7>
      <P_8A>1</P_8A>
      <P_9B>150.00</P_9B>
      <P_11>150.00</P_11>
    </FaWiersz>
  </Fa>
</Faktura>";
        }
    }
}
