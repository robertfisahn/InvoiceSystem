using InvoiceSystem.Infrastructure.Persistence;
using InvoiceSystem.Domain.Entities;
using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

// --- COMMAND (Zapis) ---
public record CreateInvoiceCommand : IRequest<CreateInvoiceResult>
{
    public int ContractorId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime Date { get; init; } = DateTime.Today;
    public List<CreateInvoiceItemCommand> Items { get; init; } = [];
}

public record CreateInvoiceItemCommand(string Name, decimal Quantity, decimal UnitPrice);

public record CreateInvoiceResult(bool Success, int? InvoiceId = null, string? Error = null);

// --- HANDLER ---
public class CreateInvoiceCommandHandler(AppDbContext db) 
    : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    public async Task<CreateInvoiceResult> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        try
        {
            var invoice = new Invoice
            {
                InvoiceNumber = request.InvoiceNumber,
                Date = request.Date,
                ContractorId = request.ContractorId,
                Items = request.Items.Select(i => new InvoiceItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.Quantity * i.UnitPrice
                }).ToList()
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(ct);

            return new CreateInvoiceResult(true, invoice.Id);
        }
        catch (Exception ex)
        {
            return new CreateInvoiceResult(false, null, ex.Message);
        }
    }
}
