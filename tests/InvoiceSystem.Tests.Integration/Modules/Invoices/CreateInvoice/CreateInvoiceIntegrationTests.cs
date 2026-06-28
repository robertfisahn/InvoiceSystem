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
using InvoiceSystem.Web.Modules.Invoices.Features.CreateInvoice.CreateInvoiceCommand;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Modules.Invoices.CreateInvoice
{
    public class CreateInvoiceIntegrationTests : IntegrationTestBase
    {
        private async Task<Contractor> CreateTestContractorAsync(AppDbContext db)
        {
            var contractor = new Contractor
            {
                Name = "Invoice Customer Sp. z o.o.",
                TaxId = "5250000123",
                Address = "Krakowskie Przedmieście 1, Warszawa"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();
            return contractor;
        }

        [Fact]
        public async Task CreateInvoice_ShouldSuccessfullyIntegrateWithDatabase_WhenDispatchedViaMediator()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contractor = await CreateTestContractorAsync(dbContext);

            var command = new CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = new DateTime(2026, 6, 14),
                Items = new List<CreateInvoiceItemCommand>
                {
                    new CreateInvoiceItemCommand("Consulting Service", 10, 150.00m)
                }
            };

            // Act
            var invoiceId = await mediator.Send(command, CancellationToken.None);

            // Assert
            invoiceId.Should().BeGreaterThan(0);

            var savedInvoice = await dbContext.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            savedInvoice.Should().NotBeNull();
            savedInvoice!.ContractorId.Should().Be(contractor.Id);
            savedInvoice.InvoiceNumber.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateInvoice_ShouldFail_WhenContractorDoesNotExist()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var invalidContractorId = 999999;

            var command = new CreateInvoiceCommand
            {
                ContractorId = invalidContractorId,
                Date = new DateTime(2026, 6, 14),
                Items = new List<CreateInvoiceItemCommand> { new CreateInvoiceItemCommand("Item A", 1, 10.00m) }
            };

            // Act & Assert
            Func<Task> act = async () => await mediator.Send(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Kontrahent o podanym identyfikatorze (ID: 999999) nie istnieje.");
        }
    }
}
