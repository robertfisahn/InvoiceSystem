using InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

public record UpdateInvoiceViewModel
{
    public int Id { get; init; }
    public List<ContractorLookupDto> Contractors { get; init; } = [];
    public UpdateInvoiceCommand Command { get; init; } = new();
}

public record ContractorLookupDto(int Id, string Name);
