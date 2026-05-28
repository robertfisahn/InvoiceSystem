using InvoiceSystem.Web.Features.Invoices.GetInvoiceDetails;
using InvoiceSystem.Web.Infrastructure.Ksef;

namespace InvoiceSystem.Web.Infrastructure.Services.Preview;

public interface IInvoicePreviewService
{
    InvoicePreviewDto MapFromInvoice(GetInvoiceDetailsViewModel viewModel);
    InvoicePreviewDto MapFromKsef(ParsedKsefInvoice parsed, string? ksefNumber);
}
