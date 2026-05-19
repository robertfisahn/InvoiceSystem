using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

public record CreateInvoiceCommand : IRequest<int>
{
    public int ContractorId { get; init; }
    public DateTime Date { get; init; } = DateTime.Today;
    public string? FilePath { get; init; }
    public List<CreateInvoiceItemCommand> Items { get; init; } = [];
}

public record CreateInvoiceItemCommand(string Name, decimal Quantity, decimal UnitPrice);
