using System.Collections.Generic;

namespace InvoiceSystem.Web.Modules.Invoices.Features.CreateInvoice.GetCreateInvoiceQuery;

public record CreateInvoiceViewModel
{
    public List<ContractorLookupDto> Contractors { get; init; } = [];
    public CreateInvoiceCommand.CreateInvoiceCommand Command { get; init; } = new();
}

public record ContractorLookupDto(int Id, string Name);
