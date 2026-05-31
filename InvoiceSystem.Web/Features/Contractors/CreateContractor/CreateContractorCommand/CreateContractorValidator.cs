using FluentValidation;

namespace InvoiceSystem.Web.Features.Contractors.CreateContractor.CreateContractorCommand;

public sealed class CreateContractorValidator : AbstractValidator<CreateContractorCommand>
{
    public CreateContractorValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa kontrahenta jest wymagana.")
            .MaximumLength(250).WithMessage("Nazwa kontrahenta nie może przekraczać 250 znaków.");

        RuleFor(x => x.TaxId)
            .NotEmpty().WithMessage("NIP kontrahenta jest wymagany.")
            .Matches(@"^\d{10}$|^\d{3}-\d{3}-\d{2}-\d{2}$|^\d{3}-\d{2}-\d{2}-\d{3}$")
            .WithMessage("Wprowadź poprawny, 10-cyfrowy numer NIP.");
    }
}
