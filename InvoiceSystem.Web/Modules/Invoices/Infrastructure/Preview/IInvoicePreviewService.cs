using InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceDetails;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;

namespace InvoiceSystem.Web.Modules.Invoices.Infrastructure.Preview;

public interface IInvoicePreviewService
{
    InvoicePreviewDto MapFromInvoice(GetInvoiceDetailsViewModel viewModel);
    InvoicePreviewDto MapFromKsef(ParsedKsefInvoice parsed, string? ksefNumber);
}
