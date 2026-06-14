using System;
using System.Collections.Generic;
using FluentValidation.TestHelper;
using InvoiceSystem.Web.Features.Invoices.CreateInvoice.CreateInvoiceCommand;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Features.Invoices.CreateInvoice
{
    public class CreateInvoiceValidatorTests
    {
        private readonly CreateInvoiceValidator _validator;

        public CreateInvoiceValidatorTests()
        {
            _validator = new CreateInvoiceValidator();
        }

        [Fact]
        public void Should_Be_Valid_When_Command_Is_Correct()
        {
            // Arrange
            var command = new CreateInvoiceCommand
            {
                ContractorId = 1,
                Date = DateTime.Today,
                Items = new List<CreateInvoiceItemCommand>
                {
                    new CreateInvoiceItemCommand("Product A", 2, 50.00m)
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Should_Have_Error_When_ContractorId_Is_Invalid(int invalidContractorId)
        {
            // Arrange
            var command = new CreateInvoiceCommand { ContractorId = invalidContractorId };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ContractorId)
                  .WithErrorMessage("Wybierz kontrahenta.");
        }

        [Fact]
        public void Should_Have_Error_When_Date_Is_Empty()
        {
            // Arrange
            var command = new CreateInvoiceCommand { Date = default };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Date)
                  .WithErrorMessage("Data jest wymagana.");
        }

        [Fact]
        public void Should_Have_Error_When_Items_Are_Empty()
        {
            // Arrange
            var command = new CreateInvoiceCommand
            {
                Items = new List<CreateInvoiceItemCommand>()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Items)
                  .WithErrorMessage("Faktura musi mieć przynajmniej jedną pozycję.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Should_Have_Error_When_Item_Name_Is_Empty(string? invalidName)
        {
            // Arrange
            var command = new CreateInvoiceCommand
            {
                Items = new List<CreateInvoiceItemCommand>
                {
                    new CreateInvoiceItemCommand(invalidName!, 1, 10.00m)
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Items[0].Name")
                  .WithErrorMessage("Nazwa pozycji jest wymagana.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Should_Have_Error_When_Item_Quantity_Is_Invalid(int invalidQuantity)
        {
            // Arrange
            var command = new CreateInvoiceCommand
            {
                Items = new List<CreateInvoiceItemCommand>
                {
                    new CreateInvoiceItemCommand("Product A", invalidQuantity, 10.00m)
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Items[0].Quantity")
                  .WithErrorMessage("Ilość musi być większa od 0.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10.50)]
        public void Should_Have_Error_When_Item_UnitPrice_Is_Invalid(double invalidPriceDouble)
        {
            // Arrange
            decimal invalidPrice = (decimal)invalidPriceDouble;
            var command = new CreateInvoiceCommand
            {
                Items = new List<CreateInvoiceItemCommand>
                {
                    new CreateInvoiceItemCommand("Product A", 1, invalidPrice)
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
                  .WithErrorMessage("Cena musi być większa od 0.");
        }
    }
}
