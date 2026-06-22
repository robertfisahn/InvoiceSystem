using FluentValidation;

namespace InvoiceSystem.Web.Modules.Invoices.Features.UpdateInvoice.UpdateInvoiceCommand;

public sealed class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

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
