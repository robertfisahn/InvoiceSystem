using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Features.Auth.Login;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Auth.Login
{
    public class LoginHandlerTests
    {
        private readonly SignInManager<AppUser> _signInManagerMock;
        private readonly LoginHandler _handler;

        public LoginHandlerTests()
        {
            var userStoreMock = Substitute.For<IUserStore<AppUser>>();
            var userManagerMock = Substitute.For<UserManager<AppUser>>(
                userStoreMock,
                null, // optionsAccessor
                null, // passwordHasher
                null, // userValidators
                null, // passwordValidators
                null, // keyNormalizer
                null, // errors
                null, // services
                null  // logger
            );

            var contextAccessorMock = Substitute.For<IHttpContextAccessor>();
            var claimsFactoryMock = Substitute.For<IUserClaimsPrincipalFactory<AppUser>>();
            var optionsMock = Substitute.For<IOptions<IdentityOptions>>();
            
            // Provide some default identity options to prevent null reference issues
            optionsMock.Value.Returns(new IdentityOptions());

            var loggerMock = Substitute.For<ILogger<SignInManager<AppUser>>>();
            var schemeProviderMock = Substitute.For<IAuthenticationSchemeProvider>();
            var userConfirmationMock = Substitute.For<IUserConfirmation<AppUser>>();

            _signInManagerMock = Substitute.For<SignInManager<AppUser>>(
                userManagerMock,
                contextAccessorMock,
                claimsFactoryMock,
                optionsMock,
                loggerMock,
                schemeProviderMock,
                userConfirmationMock
            );

            _handler = new LoginHandler(_signInManagerMock);
        }

        [Fact]
        public async Task Handle_Should_Return_Success_When_SignIn_Succeeds()
        {
            // Arrange
            var command = new LoginCommand("admin", "password123", false);
            
            _signInManagerMock.PasswordSignInAsync("admin", "password123", false, lockoutOnFailure: true)
                .Returns(SignInResult.Success);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Error.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Should_Return_LockedOut_When_Account_Is_Locked()
        {
            // Arrange
            var command = new LoginCommand("admin", "password123", false);
            
            _signInManagerMock.PasswordSignInAsync("admin", "password123", false, lockoutOnFailure: true)
                .Returns(SignInResult.LockedOut);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Konto jest zablokowane. Spróbuj ponownie później.");
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Credentials_Are_Incorrect()
        {
            // Arrange
            var command = new LoginCommand("admin", "wrong_pass", false);
            
            _signInManagerMock.PasswordSignInAsync("admin", "wrong_pass", false, lockoutOnFailure: true)
                .Returns(SignInResult.Failed);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Be("Nieprawidłowa nazwa użytkownika lub hasło.");
        }
    }
}
