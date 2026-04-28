using FluentValidation;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceList;

public class GetInvoiceListValidator : AbstractValidator<GetInvoiceListQuery>
{
    public GetInvoiceListValidator()
    {
        // Query nie ma parametrów, więc walidacja jest pusta, 
        // ale plik musi istnieć zgodnie z kontraktem VSA.
    }
}
