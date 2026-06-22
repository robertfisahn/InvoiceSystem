using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Modules.Auth.Domain;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Invoices.Features.ConfirmInvoice;
using InvoiceSystem.Web.Modules.Invoices.Features.DeleteInvoice;
using InvoiceSystem.Web.Modules.Invoices.Features.MarkAsPaid;
using InvoiceSystem.Web.Modules.Invoices.Features.UpdateInvoice.UpdateInvoiceCommand;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Modules.Invoices
{
    public class InvoiceLifecycleIntegrationTests : IntegrationTestBase
    {
        private async Task<(Contractor, Invoice)> PrepareTestInvoiceAsync(AppDbContext db, InvoiceStatus initialStatus)
        {
            var contractor = new Contractor
            {
                Name = "Lifecycle Customer Sp. z o.o.",
                TaxId = "5250000456",
                Address = "Jasna 5, Warszawa"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var invoice = new Invoice
            {
                ContractorId = contractor.Id,
                InvoiceNumber = $"INV/TEST/{Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper()}",
                Date = DateTime.UtcNow,
                Status = initialStatus,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem
                    {
                        Name = "Initial Item 1",
                        Quantity = 2,
                        UnitPrice = 50m,
                        TotalPrice = 100m
                    },
                    new InvoiceItem
                    {
                        Name = "Initial Item 2",
                        Quantity = 1,
                        UnitPrice = 200m,
                        TotalPrice = 200m
                    }
                }
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            return (contractor, invoice);
        }

        [Fact]
        public async Task ConfirmInvoice_ShouldChangeStatusToConfirmed_WhenInvoiceIsDraft()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Draft);
            var command = new ConfirmInvoiceCommand(invoice.Id);

            // Act
            await mediator.Send(command, CancellationToken.None);

            // Assert
            await db.Entry(invoice).ReloadAsync();
            invoice.Status.Should().Be(InvoiceStatus.Confirmed);
        }

        [Fact]
        public async Task ConfirmInvoice_ShouldThrowInvalidOperationException_WhenInvoiceIsNotDraft()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Confirmed);
            var command = new ConfirmInvoiceCommand(invoice.Id);

            // Act & Assert
            Func<Task> act = async () => await mediator.Send(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Można zatwierdzić wyłącznie faktury o statusie Szkic (Draft).");
        }

        [Fact]
        public async Task DeleteInvoice_ShouldRemoveFromDatabase_WhenInvoiceIsDraft()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Draft);
            var command = new DeleteInvoiceCommand(invoice.Id);

            // Act
            await mediator.Send(command, CancellationToken.None);

            // Assert
            var deleted = await db.Invoices.FindAsync(invoice.Id);
            deleted.Should().BeNull();

            // Verify items deleted as cascade
            var itemsCount = await db.InvoiceItems.Where(i => i.InvoiceId == invoice.Id).CountAsync();
            itemsCount.Should().Be(0);
        }

        [Fact]
        public async Task DeleteInvoice_ShouldThrowInvalidOperationException_WhenInvoiceIsConfirmed()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Confirmed);
            var command = new DeleteInvoiceCommand(invoice.Id);

            // Act & Assert
            Func<Task> act = async () => await mediator.Send(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Można usuwać wyłącznie faktury o statusie Szkic (Draft).");
        }

        [Fact]
        public async Task MarkAsPaid_ShouldChangeStatusToPaid_WhenInvoiceIsConfirmed()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Confirmed);
            var command = new MarkAsPaidCommand(invoice.Id);

            // Act
            await mediator.Send(command, CancellationToken.None);

            // Assert
            await db.Entry(invoice).ReloadAsync();
            invoice.Status.Should().Be(InvoiceStatus.Paid);
        }

        [Fact]
        public async Task MarkAsPaid_ShouldThrowInvalidOperationException_WhenInvoiceIsDraft()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Draft);
            var command = new MarkAsPaidCommand(invoice.Id);

            // Act & Assert
            Func<Task> act = async () => await mediator.Send(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Można oznaczyć jako opłaconą wyłącznie fakturę w statusie Zatwierdzona (Confirmed).");
        }

        [Fact]
        public async Task UpdateInvoice_ShouldSynchronizeHeaderAndItemsCorrectly_WhenInvoiceIsDraft()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Draft);

            // Let's retrieve items so we know their IDs to modify
            var items = invoice.Items.ToList();
            var itemToModify = items[0];
            var itemToRemove = items[1];

            // Setup update command:
            // 1. Change InvoiceNumber and Date
            // 2. Modify itemToModify (Quantity 2 -> 5, Price 50 -> 60)
            // 3. Omit itemToRemove (it should be deleted)
            // 4. Add a brand new item
            var command = new UpdateInvoiceCommand
            {
                Id = invoice.Id,
                InvoiceNumber = "FV/UPDATED/2026",
                Date = new DateTime(2026, 12, 31),
                ContractorId = invoice.ContractorId,
                Items = new List<UpdateInvoiceItemCommand>
                {
                    new UpdateInvoiceItemCommand
                    {
                        Id = itemToModify.Id,
                        Name = "Modified Item 1",
                        Quantity = 5,
                        UnitPrice = 60m
                    },
                    new UpdateInvoiceItemCommand
                    {
                        Id = null, // New item
                        Name = "Brand New Item",
                        Quantity = 3,
                        UnitPrice = 15m
                    }
                }
            };

            // Act
            await mediator.Send(command, CancellationToken.None);

            // Assert
            var updated = await db.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == invoice.Id);

            updated.Should().NotBeNull();
            updated!.InvoiceNumber.Should().Be("FV/UPDATED/2026");
            updated.Date.Should().Be(new DateTime(2026, 12, 31));

            // Verify item synchronization
            updated.Items.Count.Should().Be(2);

            var modified = updated.Items.FirstOrDefault(i => i.Id == itemToModify.Id);
            modified.Should().NotBeNull();
            modified!.Name.Should().Be("Modified Item 1");
            modified.Quantity.Should().Be(5);
            modified.UnitPrice.Should().Be(60m);
            modified.TotalPrice.Should().Be(300m); // 5 * 60

            var brandNew = updated.Items.FirstOrDefault(i => i.Id != itemToModify.Id);
            brandNew.Should().NotBeNull();
            brandNew!.Name.Should().Be("Brand New Item");
            brandNew.Quantity.Should().Be(3);
            brandNew.UnitPrice.Should().Be(15m);
            brandNew.TotalPrice.Should().Be(45m); // 3 * 15

            // Verify the omitted item is gone
            var deletedItemExists = await db.InvoiceItems.AnyAsync(i => i.Id == itemToRemove.Id);
            deletedItemExists.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateInvoice_ShouldThrowInvalidOperationException_WhenInvoiceIsNotDraft()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (_, invoice) = await PrepareTestInvoiceAsync(db, InvoiceStatus.Confirmed);

            var command = new UpdateInvoiceCommand
            {
                Id = invoice.Id,
                InvoiceNumber = "FV/UPDATED/FAIL",
                Date = DateTime.UtcNow,
                ContractorId = invoice.ContractorId,
                Items = new List<UpdateInvoiceItemCommand>
                {
                    new UpdateInvoiceItemCommand
                    {
                        Name = "Item X",
                        Quantity = 1,
                        UnitPrice = 100m
                    }
                }
            };

            // Act & Assert
            Func<Task> act = async () => await mediator.Send(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Można edytować wyłącznie faktury o statusie Szkic (Draft).");
        }
    }
}
