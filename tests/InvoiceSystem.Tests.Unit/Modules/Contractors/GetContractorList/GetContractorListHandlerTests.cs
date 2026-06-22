using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Modules.Contractors.Features.GetContractorList;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Contractors.GetContractorList
{
    public class GetContractorListHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;

        public GetContractorListHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_List_When_No_Contractors_Exist()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new GetContractorListHandler(db);
            var query = new GetContractorListQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_Return_Ordered_List_Of_Contractors_With_Soap_Logs()
        {
            // Arrange
            using var db = _fixture.CreateContext();

            var contractorB = new Contractor
            {
                Name = "Company B",
                TaxId = "2222222222",
                Address = "Address B"
            };

            var contractorA = new Contractor
            {
                Name = "Company A",
                TaxId = "1111111111",
                Address = "Address A"
            };

            db.Contractors.AddRange(contractorB, contractorA);
            await db.SaveChangesAsync();

            // Add SoapVerificationLogs for Company B
            var logOlder = new SoapVerificationLog
            {
                Id = Guid.NewGuid(),
                ContractorId = contractorB.Id,
                NipQueried = "2222222222",
                RequestMethod = "WCF",
                RequestEnvelope = "req1",
                ResponseEnvelope = "res1",
                IsValid = false,
                VerificationCode = "NOT_FOUND",
                Timestamp = DateTime.UtcNow.AddMinutes(-10)
            };

            var logNewer = new SoapVerificationLog
            {
                Id = Guid.NewGuid(),
                ContractorId = contractorB.Id,
                NipQueried = "2222222222",
                RequestMethod = "WCF",
                RequestEnvelope = "req2",
                ResponseEnvelope = "res2",
                IsValid = true,
                VerificationCode = "ACTIVE",
                Timestamp = DateTime.UtcNow
            };

            db.SoapVerificationLogs.AddRange(logOlder, logNewer);
            await db.SaveChangesAsync();

            var handler = new GetContractorListHandler(db);
            var query = new GetContractorListQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);

            // Verify order by Name (Company A first, then Company B)
            result[0].Name.Should().Be("Company A");
            result[0].TaxId.Should().Be("1111111111");
            result[0].Address.Should().Be("Address A");
            result[0].LatestVatStatus.Should().BeNull();
            result[0].LatestVerificationDate.Should().BeNull();

            result[1].Name.Should().Be("Company B");
            result[1].TaxId.Should().Be("2222222222");
            result[1].Address.Should().Be("Address B");
            result[1].LatestVatStatus.Should().Be("ACTIVE"); // Newest status code
            result[1].LatestVerificationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }
}
