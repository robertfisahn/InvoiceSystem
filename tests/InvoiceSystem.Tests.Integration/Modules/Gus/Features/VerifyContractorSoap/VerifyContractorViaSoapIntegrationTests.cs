using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Tests.Integration.Helpers;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Modules.Gus.Features.VerifyContractorSoap;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Modules.Gus.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Modules.Gus.Features.VerifyContractorSoap
{
    public class VerifyContractorViaSoapIntegrationTests : IntegrationTestBase
    {
        private HttpContextAccessor CreateMockHttpContextAccessor()
        {
            var mockAccessor = new HttpContextAccessor();
            var context = new DefaultHttpContext();
            context.Request.Scheme = "http";
            
            var uri = new Uri(_soapMockServerUrl);
            context.Request.Host = new HostString(uri.Authority);
            mockAccessor.HttpContext = context;
            
            return mockAccessor;
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
            Func<Task> act = async () => await client.Zaloguj("invalid_key_123");
            await act.Should().ThrowAsync<FaultException>()
                .WithMessage("Invalid user key (pKluczUzytkownika).");
        }

        [Fact]
        public async Task VerifyContractorViaSoap_ShouldLogActiveVatAndReturnSuccess_WhenNipIsActive()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contractor = new Contractor
            {
                Name = "Active Soap Contractor",
                TaxId = "2222222222",
                Address = "Gdańsk, 80-001"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var mockAccessor = CreateMockHttpContextAccessor();
            var handler = new VerifyContractorViaSoapCommandHandler(db, mockAccessor);

            // Act
            var result = await handler.Handle(new VerifyContractorViaSoapCommand(contractor.Id), CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusVat.Should().Be("ACTIVE");
            result.Name.Should().Be("Firma Testowa NIP 222 (Aktywny VAT)");

            // Verify Log
            var log = await db.SoapVerificationLogs.FirstOrDefaultAsync(l => l.ContractorId == contractor.Id);
            log.Should().NotBeNull();
            log!.NipQueried.Should().Be("2222222222");
            log.IsValid.Should().BeTrue();
            log.VerificationCode.Should().Be("ACTIVE");
            log.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task VerifyContractorViaSoap_ShouldLogNotFoundAndReturnNotRegistered_WhenNipDoesNotExistInRegistry()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contractor = new Contractor
            {
                Name = "NotFound Soap Contractor",
                TaxId = "5555555555",
                Address = "Somewhere"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var mockAccessor = CreateMockHttpContextAccessor();
            var handler = new VerifyContractorViaSoapCommandHandler(db, mockAccessor);

            // Act
            var result = await handler.Handle(new VerifyContractorViaSoapCommand(contractor.Id), CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.StatusVat.Should().Be("NOT_FOUND");
            result.Name.Should().BeNull();

            // Verify Log
            var log = await db.SoapVerificationLogs.FirstOrDefaultAsync(l => l.ContractorId == contractor.Id);
            log.Should().NotBeNull();
            log!.NipQueried.Should().Be("5555555555");
            log.IsValid.Should().BeFalse();
            log.VerificationCode.Should().Be("NOT_FOUND");
            log.ErrorMessage.Should().Be("Nie znaleziono podmiotu dla podanych kryteriów wyszukiwania.");
        }

        [Fact]
        public async Task VerifyContractorViaSoap_ShouldLogFailureAndReturnErrorResult_WhenSoapFaultExceptionOccurs()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var contractor = new Contractor
            {
                Name = "Fault Soap Contractor",
                TaxId = "9999999999",
                Address = "Fault street"
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();

            var mockAccessor = CreateMockHttpContextAccessor();
            var handler = new VerifyContractorViaSoapCommandHandler(db, mockAccessor);

            // Act
            var result = await handler.Handle(new VerifyContractorViaSoapCommand(contractor.Id), CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("GUS service error");

            // Verify Log
            var log = await db.SoapVerificationLogs.FirstOrDefaultAsync(l => l.ContractorId == contractor.Id);
            log.Should().NotBeNull();
            log!.NipQueried.Should().Be("9999999999");
            log.IsValid.Should().BeFalse();
            log.VerificationCode.Should().Be("ERROR");
            log.ErrorMessage.Should().Contain("Internal server error or company registry search failure.");
        }
    }
}
