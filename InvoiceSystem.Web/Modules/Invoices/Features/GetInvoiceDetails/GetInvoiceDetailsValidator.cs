using FluentValidation;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceDetails;

public sealed class GetInvoiceDetailsValidator : AbstractValidator<GetInvoiceDetailsQuery>
{
    public GetInvoiceDetailsValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Nieprawidłowy identyfikator faktury.");
    }
}
