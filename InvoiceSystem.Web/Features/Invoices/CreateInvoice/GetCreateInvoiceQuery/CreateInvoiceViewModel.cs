using System.Collections.Generic;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice.GetCreateInvoiceQuery;

public record CreateInvoiceViewModel
{
    public List<ContractorLookupDto> Contractors { get; init; } = [];
    public CreateInvoiceCommand.CreateInvoiceCommand Command { get; init; } = new();
}

public record ContractorLookupDto(int Id, string Name);
