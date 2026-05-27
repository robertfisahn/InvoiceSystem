using InvoiceSystem.Web.Features.Invoices.GetInvoiceDetails;
using InvoiceSystem.Web.Infrastructure.Ksef;
using InvoiceSystem.Web.Shared.Models;

namespace InvoiceSystem.Web.Shared.Interfaces;

public interface IInvoicePreviewService
{
    InvoicePreviewDto MapFromInvoice(GetInvoiceDetailsViewModel viewModel);
    InvoicePreviewDto MapFromKsef(ParsedKsefInvoice parsed, string? ksefNumber);
}
