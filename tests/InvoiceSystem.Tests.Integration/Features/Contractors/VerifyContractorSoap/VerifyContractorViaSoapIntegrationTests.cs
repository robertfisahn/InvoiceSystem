using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Contractors.VerifyContractorSoap;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Soap;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Features.Contractors.VerifyContractorSoap
{
    public class VerifyContractorViaSoapIntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task Test_SoapMock_Success_Login_And_Search()
        {
            // Arrange
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress($"{_soapMockServerUrl}/services/regon.asmx");
            var factory = new ChannelFactory<IUslugaBIRzewnPubl>(binding, endpoint);
            var client = factory.CreateChannel();

            // Act
            var sid = await client.Zaloguj("abcde12345abcde12345");
            sid.Should().Be("mock_session_sid_999");

            using (new OperationContextScope((IContextChannel)client))
            {
                var requestProperty = new System.ServiceModel.Channels.HttpRequestMessageProperty();
                requestProperty.Headers["sid"] = sid;
                OperationContext.Current.OutgoingMessageProperties[System.ServiceModel.Channels.HttpRequestMessageProperty.Name] = requestProperty;

                var resultXml = await client.DaneSzukajJednostki(new ParametryWyszukiwania { Nip = "2222222222" });
                
                // Assert
                resultXml.Should().NotBeNull();
                resultXml.Should().Contain("Firma Testowa NIP 222 (Aktywny VAT)");
            }

            var logoutResult = await client.Wyloguj(sid);
            logoutResult.Should().BeTrue();
        }

        [Fact]
        public async Task Test_SoapMock_InvalidApiKey_ThrowsFaultException()
        {
            // Arrange
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress($"{_soapMockServerUrl}/services/regon.asmx");
            var factory = new ChannelFactory<IUslugaBIRzewnPubl>(binding, endpoint);
            var client = factory.CreateChannel();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FaultException>(async () =>
            {
                await client.Zaloguj("invalid_key_123");
            });

            ex.Message.Should().Contain("Invalid user key");
        }

        [Fact]
        public async Task Test_Handler_VerifyContractorViaSoapCommand_Success()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var contractor = new Contractor
            {
                Name = "Pending Verification Co",
                TaxId = "2222222222",
                Address = "Test street 123"
            };
            dbContext.Contractors.Add(contractor);
            await dbContext.SaveChangesAsync();

            var context = new DefaultHttpContext();
            context.Request.Scheme = "http";
            var uri = new Uri(_soapMockServerUrl);
            context.Request.Host = new HostString(uri.Authority);
            httpContextAccessor.HttpContext = context;

            // Act
            var result = await mediator.Send(new VerifyContractorViaSoapCommand(contractor.Id), CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Name.Should().Be("Firma Testowa NIP 222 (Aktywny VAT)");
            result.StatusVat.Should().Be("ACTIVE");

            var log = await dbContext.SoapVerificationLogs.FirstOrDefaultAsync(l => l.ContractorId == contractor.Id);
            log.Should().NotBeNull();
            log!.NipQueried.Should().Be("2222222222");
            log.VerificationCode.Should().Be("ACTIVE");
            log.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Test_Handler_VerifyContractorViaSoapCommand_NotFound()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var contractor = new Contractor
            {
                Name = "Unknown Company",
                TaxId = "5555555555",
                Address = "Some Address"
            };
            dbContext.Contractors.Add(contractor);
            await dbContext.SaveChangesAsync();

            var context = new DefaultHttpContext();
            context.Request.Scheme = "http";
            var uri = new Uri(_soapMockServerUrl);
            context.Request.Host = new HostString(uri.Authority);
            httpContextAccessor.HttpContext = context;

            // Act
            var result = await mediator.Send(new VerifyContractorViaSoapCommand(contractor.Id), CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.Name.Should().BeNull();
            result.StatusVat.Should().Be("NOT_FOUND");

            var log = await dbContext.SoapVerificationLogs.FirstOrDefaultAsync(l => l.ContractorId == contractor.Id);
            log.Should().NotBeNull();
            log!.NipQueried.Should().Be("5555555555");
            log.VerificationCode.Should().Be("NOT_FOUND");
            log.IsValid.Should().BeFalse();
        }
    }
}
