using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

public sealed class CreateInvoiceHandler(AppDbContext db)
    : IRequestHandler<CreateInvoiceCommand, int>
{
    public async Task<int> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
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

        return invoice.Id;
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
