using CommandClass = InvoiceSystem.Web.Modules.Invoices.Features.UpdateInvoice.UpdateInvoiceCommand.UpdateInvoiceCommand;

namespace InvoiceSystem.Web.Modules.Invoices.Features.UpdateInvoice;

public record UpdateInvoiceViewModel
{
    public int Id { get; init; }
    public List<ContractorLookupDto> Contractors { get; init; } = [];
    public CommandClass Command { get; init; } = new();
}

public record ContractorLookupDto(int Id, string Name);


