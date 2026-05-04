using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

public record UpdateInvoiceCommand : IRequest<UpdateInvoiceResult>
{
    public int Id { get; init; }
    public int ContractorId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime Date { get; init; } = DateTime.Today;
    public List<UpdateInvoiceItemCommand> Items { get; init; } = [];
}

public record UpdateInvoiceItemCommand
{
    public int? Id { get; init; } // null dla nowych pozycji
    public string Name { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public record UpdateInvoiceResult(bool Success, string? Error = null);
