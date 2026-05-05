using InvoiceSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.DeleteInvoice;

// --- COMMAND ---
public record DeleteInvoiceCommand(int Id) : IRequest<DeleteInvoiceResult>;

public record DeleteInvoiceResult(bool Success, string? Error = null);

// --- HANDLER ---
public class DeleteInvoiceCommandHandler(AppDbContext db) 
    : IRequestHandler<DeleteInvoiceCommand, DeleteInvoiceResult>
{
    public async Task<DeleteInvoiceResult> Handle(DeleteInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (invoice == null)
        {
            return new DeleteInvoiceResult(false, "Faktura nie istnieje.");
        }

        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);

        return new DeleteInvoiceResult(true);
    }
}
