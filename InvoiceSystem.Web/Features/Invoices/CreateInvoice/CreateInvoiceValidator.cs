using FluentValidation;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.ContractorId)
            .GreaterThan(0)
            .WithMessage("Wybierz kontrahenta.");

        RuleFor(x => x.InvoiceNumber)
            .NotEmpty()
            .WithMessage("Numer faktury jest wymagany.");

        RuleFor(x => x.Date)
            .NotEmpty()
            .WithMessage("Data jest wymagana.");
        
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Faktura musi mieć przynajmniej jedną pozycję.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Name)
                .NotEmpty()
                .WithMessage("Nazwa pozycji jest wymagana.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0)
                .WithMessage("Ilość musi być większa od 0.");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0)
                .WithMessage("Cena musi być większa od 0.");
        });
    }
}
