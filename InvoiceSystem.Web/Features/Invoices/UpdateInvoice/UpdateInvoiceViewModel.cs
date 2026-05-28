using CommandClass = InvoiceSystem.Web.Features.Invoices.UpdateInvoice.UpdateInvoiceCommand.UpdateInvoiceCommand;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

public record UpdateInvoiceViewModel
{
    public int Id { get; init; }
    public List<ContractorLookupDto> Contractors { get; init; } = [];
    public CommandClass Command { get; init; } = new();
}

public record ContractorLookupDto(int Id, string Name);


