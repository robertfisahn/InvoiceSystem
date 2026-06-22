using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Unit.Common;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Modules.Gus.Features.VerifyContractorSoap;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Gus.Features.VerifyContractorSoap
{
    public class VerifyContractorViaSoapCommandHandlerTests : IDisposable
    {
        private readonly SqliteDbContextFixture _fixture;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VerifyContractorViaSoapCommandHandlerTests()
        {
            _fixture = new SqliteDbContextFixture();
            _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Contractor_Not_Found()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var handler = new VerifyContractorViaSoapCommandHandler(db, _httpContextAccessor);
            var command = new VerifyContractorViaSoapCommand(999);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Contractor not found.");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Contractor_Has_No_TaxId()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor
            {
                Name = "Acme",
                TaxId = "", // Missing TaxId
                Address = "Some address"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var handler = new VerifyContractorViaSoapCommandHandler(db, _httpContextAccessor);
            var command = new VerifyContractorViaSoapCommand(contractor.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Contractor has no tax identifier (NIP).");
        }

        [Fact]
        public async Task Handle_Should_Return_Error_When_Soap_Connection_Fails_During_Channel_Creation()
        {
            // Arrange
            using var db = _fixture.CreateContext();
            var contractor = new Contractor
            {
                Name = "Acme",
                TaxId = "1234567890",
                Address = "Some address"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            // Setup a mock HttpContext with an invalid SOAP endpoint URL to guarantee WCF failure
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("invalid-host-name-for-testing");
            _httpContextAccessor.HttpContext.Returns(httpContext);

            var handler = new VerifyContractorViaSoapCommandHandler(db, _httpContextAccessor);
            var command = new VerifyContractorViaSoapCommand(contractor.Id);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("error");
        }
    }
}
