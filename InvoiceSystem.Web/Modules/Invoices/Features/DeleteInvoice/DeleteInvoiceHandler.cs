using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Modules.Invoices.Features.DeleteInvoice;

public sealed class DeleteInvoiceHandler(AppDbContext db)
    : IRequestHandler<DeleteInvoiceCommand, Unit>
{
    public async Task<Unit> Handle(DeleteInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new InvalidOperationException($"Invoice {request.Id} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Można usuwać wyłącznie faktury o statusie Szkic (Draft).");
        }

        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
