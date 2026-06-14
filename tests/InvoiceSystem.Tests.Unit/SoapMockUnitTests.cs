using System;
using System.ServiceModel;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Soap;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace InvoiceSystem.Tests.Unit
{
    public class SoapMockUnitTests
    {
        [Fact]
        public async Task Test_Zaloguj_Success_WithCorrectKey()
        {
            // Arrange
            var mockService = new UslugaBIRzewnPublMock(new HttpContextAccessor());
            string correctKey = "abcde12345abcde12345";

            // Act
            string sid = await mockService.Zaloguj(correctKey);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(sid));
            Assert.Contains("mock_session_sid_", sid);
        }

        [Fact]
        public async Task Test_Zaloguj_ThrowsFaultException_WithInvalidKey()
        {
            // Arrange
            var mockService = new UslugaBIRzewnPublMock(new HttpContextAccessor());
            string invalidKey = "wrong_key";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<FaultException>(() => mockService.Zaloguj(invalidKey));
            Assert.Equal("Invalid user key (pKluczUzytkownika).", exception.Message);
        }

        [Fact]
        public async Task Test_Wyloguj_Success()
        {
            // Arrange
            var mockService = new UslugaBIRzewnPublMock(new HttpContextAccessor());
            string activeSid = "mock_session_sid_999";

            // Act
            bool result = await mockService.Wyloguj(activeSid);

            // Assert
            Assert.True(result);
        }
    }
}
