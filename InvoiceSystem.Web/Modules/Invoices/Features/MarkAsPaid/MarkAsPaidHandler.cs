using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Modules.Invoices.Features.MarkAsPaid;

public sealed class MarkAsPaidHandler(AppDbContext db)
    : IRequestHandler<MarkAsPaidCommand, Unit>
{
    public async Task<Unit> Handle(MarkAsPaidCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new InvalidOperationException($"Faktura o identyfikatorze {request.Id} nie została znaleziona.");

        if (invoice.Status != InvoiceStatus.Confirmed)
        {
            throw new InvalidOperationException("Można oznaczyć jako opłaconą wyłącznie fakturę w statusie Zatwierdzona (Confirmed).");
        }

        invoice.Status = InvoiceStatus.Paid;
        await db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
