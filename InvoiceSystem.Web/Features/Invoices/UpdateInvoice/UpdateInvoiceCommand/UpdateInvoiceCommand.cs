using System;
using System.Collections.Generic;
using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice.UpdateInvoiceCommand;

public record UpdateInvoiceCommand : IRequest<Unit>
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
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
