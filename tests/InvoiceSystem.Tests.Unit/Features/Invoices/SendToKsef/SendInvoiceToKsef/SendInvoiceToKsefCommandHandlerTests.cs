using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Invoices.SendToKsef.SendInvoiceToKsef;
using InvoiceSystem.Web.Infrastructure.Ksef;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Invoices.SendToKsef.SendInvoiceToKsef
{
    public class SendInvoiceToKsefCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IKsefClient _ksefClient;

        public SendInvoiceToKsefCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _ksefClient = Substitute.For<IKsefClient>();
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
            var handler = new SendInvoiceToKsefCommandHandler(db, _ksefClient);
            var command = new SendInvoiceToKsefCommand(999);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Faktura nie istnieje.");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Invoice_Status_Not_Confirmed()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Draft, // Not Confirmed
                ContractorId = contractor.Id
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var handler = new SendInvoiceToKsefCommandHandler(db, _ksefClient);
            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Tylko zatwierdzone faktury mogą być wysłane do KSeF.");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Invoice_Already_Has_KsefNumber()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Confirmed,
                KsefNumber = "1234567890", // Already has KsefNumber
                ContractorId = contractor.Id
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var handler = new SendInvoiceToKsefCommandHandler(db, _ksefClient);
            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.KsefNumber.Should().Be("1234567890");
            result.ErrorMessage.Should().Be("Ta faktura posiada już nadany numer KSeF.");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_KsefSettings_Are_Not_Configured()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "123", Address = "Address" };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Confirmed,
                ContractorId = contractor.Id
            };
            db.Invoices.Add(invoice);
            // Notice: KsefSettings are NOT seeded
            await db.SaveChangesAsync();

            var handler = new SendInvoiceToKsefCommandHandler(db, _ksefClient);
            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Integracja z KSeF nie jest skonfigurowana. Skonfiguruj NIP i Token w ustawieniach.");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Exception_Is_Thrown()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "1234567890", Address = "Address" };
            db.Contractors.Add(contractor);
            
            var settings = new KsefSetting { Nip = "1234567890", ApiKey = "KEY-123" };
            db.KsefSettings.Add(settings);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Confirmed,
                ContractorId = contractor.Id,
                Items = new List<InvoiceItem> { new InvoiceItem { Name = "Item", Quantity = 1, UnitPrice = 10m } }
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            // Setup IKsefClient to throw exception
            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception("KSeF connection failed"));

            var handler = new SendInvoiceToKsefCommandHandler(db, _ksefClient);
            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("KSeF connection failed");
        }

        [Fact]
        public async Task Handle_Should_Return_Success_With_TransactionId_Only_When_Invoice_Not_Fully_Processed()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "1234567890", Address = "Address" };
            db.Contractors.Add(contractor);
            
            var settings = new KsefSetting { Nip = "1234567890", ApiKey = "KEY-123" };
            db.KsefSettings.Add(settings);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Confirmed,
                ContractorId = contractor.Id,
                Items = new List<InvoiceItem> { new InvoiceItem { Name = "Item", Quantity = 1, UnitPrice = 10m } }
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            const string sessionToken = "1|2|3|4|SESSION-REF";
            const string transactionId = "TX-12345";
            const string combinedTransactionId = "SESSION-REF:TX-12345";

            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "2026-06-14T12:00:00Z"));

            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "2026-06-14T12:00:00Z", Arg.Any<CancellationToken>())
                .Returns(sessionToken);

            _ksefClient.SendInvoiceAsync(sessionToken, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(transactionId);

            // Simulate "Pending" state
            _ksefClient.GetInvoiceStatusAsync(sessionToken, combinedTransactionId, Arg.Any<CancellationToken>())
                .Returns(new KsefStatusResult("Pending", null, null));

            var handler = new SendInvoiceToKsefCommandHandler(db, _ksefClient);
            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.KsefNumber.Should().BeNull();
            result.TransactionId.Should().Be(transactionId);
            result.ErrorMessage.Should().BeNull();

            // Verify invoice entity was updated with TransactionId
            var updated = await db.Invoices.FindAsync(invoice.Id);
            updated!.KsefTransactionId.Should().Be(combinedTransactionId);
            updated.KsefSentAt.Should().NotBeNull();
            updated.KsefNumber.Should().BeNull();

            // Verify session was closed
            await _ksefClient.Received(1).CloseSessionAsync(sessionToken, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_Should_Return_Success_With_KsefNumber_And_Save_Upo_When_Processed_Successfully()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor { Name = "Contractor 1", TaxId = "1234567890", Address = "Address" };
            db.Contractors.Add(contractor);
            
            var settings = new KsefSetting { Nip = "1234567890", ApiKey = "KEY-123" };
            db.KsefSettings.Add(settings);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                InvoiceNumber = "FV/2026/001",
                Status = InvoiceStatus.Confirmed,
                ContractorId = contractor.Id,
                Items = new List<InvoiceItem> { new InvoiceItem { Name = "Item", Quantity = 1, UnitPrice = 10m } }
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            const string sessionToken = "1|2|3|4|SESSION-REF";
            const string transactionId = "TX-12345";
            const string combinedTransactionId = "SESSION-REF:TX-12345";
            const string ksefNumber = "1234567890-20260614-ABCDEF-12";
            const string upoXml = "<upo>UPO_XML_CONTENT</upo>";

            _ksefClient.AuthorisationChallengeAsync("1234567890", Arg.Any<CancellationToken>())
                .Returns(new KsefChallengeResult("CHALLENGE", "2026-06-14T12:00:00Z"));

            _ksefClient.InitSessionAsync("1234567890", "KEY-123", "CHALLENGE", "2026-06-14T12:00:00Z", Arg.Any<CancellationToken>())
                .Returns(sessionToken);

            _ksefClient.SendInvoiceAsync(sessionToken, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(transactionId);

            // Simulate "Processed" state
            _ksefClient.GetInvoiceStatusAsync(sessionToken, combinedTransactionId, Arg.Any<CancellationToken>())
                .Returns(new KsefStatusResult("Processed", ksefNumber, null));

            _ksefClient.DownloadUpoAsync(sessionToken, ksefNumber, Arg.Any<CancellationToken>())
                .Returns(upoXml);

            var handler = new SendInvoiceToKsefCommandHandler(db, _ksefClient);
            var command = new SendInvoiceToKsefCommand(invoice.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.KsefNumber.Should().Be(ksefNumber);
            result.TransactionId.Should().Be(transactionId);
            result.ErrorMessage.Should().BeNull();

            // Verify invoice entity was updated with KsefNumber and UpoXml
            var updated = await db.Invoices.FindAsync(invoice.Id);
            updated!.KsefTransactionId.Should().Be(combinedTransactionId);
            updated.KsefSentAt.Should().NotBeNull();
            updated.KsefNumber.Should().Be(ksefNumber);
            updated.UpoXml.Should().Be(upoXml);

            // Verify session was closed
            await _ksefClient.Received(1).CloseSessionAsync(sessionToken, Arg.Any<CancellationToken>());
        }
    }
}
