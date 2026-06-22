using System;
using System.Collections.Generic;
using FluentValidation.TestHelper;
using InvoiceSystem.Web.Modules.Invoices.Features.UpdateInvoice.UpdateInvoiceCommand;
using Xunit;

namespace InvoiceSystem.Tests.Unit.Modules.Invoices.UpdateInvoice
{
    public class UpdateInvoiceValidatorTests
    {
        private readonly UpdateInvoiceValidator _validator;

        public UpdateInvoiceValidatorTests()
        {
            _validator = new UpdateInvoiceValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Id_Is_Zero_Or_Negative()
        {
            var command = new UpdateInvoiceCommand { Id = 0 };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Id);

            command = new UpdateInvoiceCommand { Id = -1 };
            result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Id);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Id_Is_Greater_Than_Zero()
        {
            var command = new UpdateInvoiceCommand { Id = 1 };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveValidationErrorFor(x => x.Id);
        }

        [Fact]
        public void Should_Have_Error_When_ContractorId_Is_Zero_Or_Negative()
        {
            var command = new UpdateInvoiceCommand { ContractorId = 0 };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ContractorId)
                .WithErrorMessage("Wybierz kontrahenta.");

            command = new UpdateInvoiceCommand { ContractorId = -5 };
            result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.ContractorId)
                .WithErrorMessage("Wybierz kontrahenta.");
        }

        [Fact]
        public void Should_Have_Error_When_InvoiceNumber_Is_Null_Or_Empty()
        {
            var command = new UpdateInvoiceCommand { InvoiceNumber = "" };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.InvoiceNumber)
                .WithErrorMessage("Numer faktury jest wymagany.");
        }

        [Fact]
        public void Should_Have_Error_When_Date_Is_Default()
        {
            var command = new UpdateInvoiceCommand { Date = default };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Date)
                .WithErrorMessage("Data jest wymagana.");
        }

        [Fact]
        public void Should_Have_Error_When_Items_Are_Null_Or_Empty()
        {
            var command = new UpdateInvoiceCommand { Items = new List<UpdateInvoiceItemCommand>() };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Items)
                .WithErrorMessage("Faktura musi mieć przynajmniej jedną pozycję.");
        }

        [Fact]
        public void Should_Have_Error_When_Item_Name_Is_Null_Or_Empty()
        {
            var command = new UpdateInvoiceCommand
            {
                Items = new List<UpdateInvoiceItemCommand>
                {
                    new UpdateInvoiceItemCommand { Name = "", Quantity = 1, UnitPrice = 10.00m }
                }
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor("Items[0].Name")
                .WithErrorMessage("Nazwa pozycji jest wymagana.");
        }

        [Fact]
        public void Should_Have_Error_When_Item_Quantity_Is_Zero_Or_Negative()
        {
            var command = new UpdateInvoiceCommand
            {
                Items = new List<UpdateInvoiceItemCommand>
                {
                    new UpdateInvoiceItemCommand { Name = "Item", Quantity = 0, UnitPrice = 10.00m },
                    new UpdateInvoiceItemCommand { Name = "Item2", Quantity = -2, UnitPrice = 10.00m }
                }
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor("Items[0].Quantity")
                .WithErrorMessage("Ilość musi być większa od 0.");
            result.ShouldHaveValidationErrorFor("Items[1].Quantity")
                .WithErrorMessage("Ilość musi być większa od 0.");
        }

        [Fact]
        public void Should_Have_Error_When_Item_UnitPrice_Is_Zero_Or_Negative()
        {
            var command = new UpdateInvoiceCommand
            {
                Items = new List<UpdateInvoiceItemCommand>
                {
                    new UpdateInvoiceItemCommand { Name = "Item", Quantity = 1, UnitPrice = 0m },
                    new UpdateInvoiceItemCommand { Name = "Item2", Quantity = 1, UnitPrice = -5.50m }
                }
            };
            var result = _validator.TestValidate(command);
            result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
                .WithErrorMessage("Cena musi być większa od 0.");
            result.ShouldHaveValidationErrorFor("Items[1].UnitPrice")
                .WithErrorMessage("Cena musi być większa od 0.");
        }

        [Fact]
        public void Should_Not_Have_Errors_For_Valid_Command()
        {
            var command = new UpdateInvoiceCommand
            {
                Id = 42,
                ContractorId = 1,
                InvoiceNumber = "FV/2026/001",
                Date = DateTime.Today,
                Items = new List<UpdateInvoiceItemCommand>
                {
                    new UpdateInvoiceItemCommand { Name = "Valid Item", Quantity = 2, UnitPrice = 150.00m }
                }
            };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
