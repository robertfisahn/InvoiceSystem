using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Modules.Contractors.Features.CreateContractor.CreateContractorCommand;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Modules.Contractors.CreateContractor
{
    public class CreateContractorIntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task CreateContractor_ShouldSuccessfullyIntegrateWithDatabase_WhenDispatchedViaMediator()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var command = new CreateContractorCommand
            {
                Name = "Integration Test Company Inc",
                TaxId = "123-456-78-90",
                Address = "123 Docker Lane, SQL City"
            };

            // Act
            var result = await mediator.Send(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ContractorId.Should().BeGreaterThan(0);

            var savedContractor = await dbContext.Contractors.FindAsync(result.ContractorId);
            savedContractor.Should().NotBeNull();
            savedContractor!.Name.Should().Be("Integration Test Company Inc");
            savedContractor.TaxId.Should().Be("1234567890"); // PERSISTENCE VERIFICATION
        }
    }
}
