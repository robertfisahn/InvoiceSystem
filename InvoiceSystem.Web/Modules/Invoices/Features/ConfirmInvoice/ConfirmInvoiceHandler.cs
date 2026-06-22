using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Modules.Invoices.Features.ConfirmInvoice;

public sealed class ConfirmInvoiceHandler(AppDbContext db)
    : IRequestHandler<ConfirmInvoiceCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new InvalidOperationException($"Faktura o identyfikatorze {request.Id} nie została znaleziona.");

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Można zatwierdzić wyłącznie faktury o statusie Szkic (Draft).");
        }

        invoice.Status = InvoiceStatus.Confirmed;
        await db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
