using FluentAssertions;
using InvoiceSystem.Web.Modules.Auth.Features.Login;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Auth.Login
{
    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _validator;

        public LoginCommandValidatorTests()
        {
            _validator = new LoginCommandValidator();
        }

        [Fact]
        public void Validate_Should_Have_Error_When_Username_Is_Empty()
        {
            // Arrange
            var command = new LoginCommand("", "password123", false);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.PropertyName == "Username" && e.ErrorMessage == "Podaj nazwę użytkownika.");
        }

        [Fact]
        public void Validate_Should_Have_Error_When_Password_Is_Empty()
        {
            // Arrange
            var command = new LoginCommand("username", "", false);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.PropertyName == "Password" && e.ErrorMessage == "Podaj hasło.");
        }

        [Fact]
        public void Validate_Should_Be_Valid_When_All_Fields_Are_Provided()
        {
            // Arrange
            var command = new LoginCommand("username", "password123", true);

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
