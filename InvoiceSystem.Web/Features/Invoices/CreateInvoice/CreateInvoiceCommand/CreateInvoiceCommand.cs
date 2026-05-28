using System;
using System.Collections.Generic;
using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice.CreateInvoiceCommand;

public record CreateInvoiceCommand : IRequest<int>
{
    public int ContractorId { get; init; }
    public DateTime Date { get; init; } = DateTime.Today;
    public string? FilePath { get; init; }
    public List<CreateInvoiceItemCommand> Items { get; init; } = [];
}

public record CreateInvoiceItemCommand(string Name, int Quantity, decimal UnitPrice);
