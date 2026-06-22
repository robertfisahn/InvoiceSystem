using FluentValidation;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceList;

public sealed class GetInvoiceListValidator : AbstractValidator<GetInvoiceListQuery>
{
    public GetInvoiceListValidator()
    {
        // Query nie ma parametrów, więc walidacja jest pusta, 
        // ale plik musi istnieć zgodnie z kontraktem VSA.
    }
}
