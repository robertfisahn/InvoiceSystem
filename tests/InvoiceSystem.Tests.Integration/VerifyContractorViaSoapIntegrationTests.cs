using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Contractors.VerifyContractorSoap;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Soap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration
{
    public class VerifyContractorViaSoapIntegrationTests : IntegrationTestBase
    {
        public VerifyContractorViaSoapIntegrationTests(WebApplicationFactory<Program> factory) 
            : base(factory)
        {
        }

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
            Assert.Equal("mock_session_sid_999", sid);

            using (new OperationContextScope((IContextChannel)client))
            {
                var requestProperty = new System.ServiceModel.Channels.HttpRequestMessageProperty();
                requestProperty.Headers["sid"] = sid;
                OperationContext.Current.OutgoingMessageProperties[System.ServiceModel.Channels.HttpRequestMessageProperty.Name] = requestProperty;

                var resultXml = await client.DaneSzukajJednostki(new ParametryWyszukiwania { Nip = "2222222222" });
                
                // Assert
                Assert.NotNull(resultXml);
                Assert.Contains("Firma Testowa NIP 222 (Aktywny VAT)", resultXml);
            }

            var logoutResult = await client.Wyloguj(sid);
            Assert.True(logoutResult);
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

            Assert.Contains("Invalid user key", ex.Message);
        }

        [Fact]
        public async Task Test_Handler_VerifyContractorViaSoapCommand_Success()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contractor = new Contractor
            {
                Id = 101,
                Name = "Pending Verification Co",
                TaxId = "2222222222",
                Address = "Test street 123"
            };
            dbContext.Contractors.Add(contractor);
            await dbContext.SaveChangesAsync();

            var mockHttpContextAccessor = new HttpContextAccessor();
            var context = new DefaultHttpContext();
            context.Request.Scheme = "http";
            var uri = new Uri(_soapMockServerUrl);
            context.Request.Host = new HostString(uri.Authority);
            mockHttpContextAccessor.HttpContext = context;

            var handler = new VerifyContractorViaSoapCommandHandler(dbContext, mockHttpContextAccessor);

            // Act
            var result = await handler.Handle(new VerifyContractorViaSoapCommand(contractor.Id), CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Firma Testowa NIP 222 (Aktywny VAT)", result.Name);
            Assert.Equal("ACTIVE", result.StatusVat);

            var log = await dbContext.SoapVerificationLogs.FirstOrDefaultAsync(l => l.ContractorId == contractor.Id);
            Assert.NotNull(log);
            Assert.Equal("2222222222", log.NipQueried);
            Assert.Equal("ACTIVE", log.VerificationCode);
            Assert.True(log.IsValid);
        }

        [Fact]
        public async Task Test_Handler_VerifyContractorViaSoapCommand_NotFound()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contractor = new Contractor
            {
                Id = 102,
                Name = "Unknown Company",
                TaxId = "5555555555",
                Address = "Some Address"
            };
            dbContext.Contractors.Add(contractor);
            await dbContext.SaveChangesAsync();

            var mockHttpContextAccessor = new HttpContextAccessor();
            var context = new DefaultHttpContext();
            context.Request.Scheme = "http";
            var uri = new Uri(_soapMockServerUrl);
            context.Request.Host = new HostString(uri.Authority);
            mockHttpContextAccessor.HttpContext = context;

            var handler = new VerifyContractorViaSoapCommandHandler(dbContext, mockHttpContextAccessor);

            // Act
            var result = await handler.Handle(new VerifyContractorViaSoapCommand(contractor.Id), CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Name);
            Assert.Equal("NOT_FOUND", result.StatusVat);

            var log = await dbContext.SoapVerificationLogs.FirstOrDefaultAsync(l => l.ContractorId == contractor.Id);
            Assert.NotNull(log);
            Assert.Equal("5555555555", log.NipQueried);
            Assert.Equal("NOT_FOUND", log.VerificationCode);
            Assert.False(log.IsValid);
        }
    }
}
