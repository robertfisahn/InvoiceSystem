using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

// --- COMMAND (Zapis) ---
public record CreateInvoiceCommand : IRequest<CreateInvoiceResult>
{
    public int ContractorId { get; init; }
    public DateTime Date { get; init; } = DateTime.Today;
    public string? FilePath { get; init; }
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
        // Automatyczne generowanie numeru faktury: FV/YYYY/MM/NNN
        var invoiceNumber = await GenerateInvoiceNumberAsync(request.Date, ct);

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            Date = request.Date,
            ContractorId = request.ContractorId,
            FilePath = request.FilePath,
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

    /// <summary>
    /// Generuje kolejny numer faktury w formacie INV/YYYY/NNN.
    /// Numeracja jest sekwencyjna w ramach roku.
    /// </summary>
    private async Task<string> GenerateInvoiceNumberAsync(DateTime date, CancellationToken ct)
    {
        var prefix = $"INV/{date:yyyy}/";

        var lastNumber = await db.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(ct);

        int nextSequence = 1;
        if (lastNumber is not null)
        {
            var lastPart = lastNumber.Replace(prefix, "");
            if (int.TryParse(lastPart, out var parsed))
                nextSequence = parsed + 1;
        }

        return $"{prefix}{nextSequence:D3}";
    }
}

