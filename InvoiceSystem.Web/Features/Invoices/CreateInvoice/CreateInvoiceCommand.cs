using InvoiceSystem.Infrastructure.Persistence;
using InvoiceSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

// --- QUERY (Dane do formularza) ---
public record GetCreateInvoiceQuery : IRequest<CreateInvoiceViewModel>;

public record CreateInvoiceViewModel
{
    public List<ContractorLookupDto> Contractors { get; init; } = [];
    public CreateInvoiceCommand Command { get; init; } = new();
}

public record ContractorLookupDto(int Id, string Name);

// --- HANDLERY ---
public class CreateInvoiceHandler(AppDbContext db) 
    : IRequestHandler<GetCreateInvoiceQuery, CreateInvoiceViewModel>,
      IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    public async Task<CreateInvoiceViewModel> Handle(GetCreateInvoiceQuery request, CancellationToken ct)
    {
        var contractors = await db.Contractors
            .OrderBy(c => c.Name)
            .Select(c => new ContractorLookupDto(c.Id, c.Name))
            .ToListAsync(ct);

        return new CreateInvoiceViewModel { Contractors = contractors };
    }

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
