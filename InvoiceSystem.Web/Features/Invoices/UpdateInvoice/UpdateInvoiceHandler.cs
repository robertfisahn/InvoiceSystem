using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

public class UpdateInvoiceHandler(AppDbContext db) 
    : IRequestHandler<UpdateInvoiceCommand, UpdateInvoiceResult>
{
    public async Task<UpdateInvoiceResult> Handle(UpdateInvoiceCommand request, CancellationToken ct)
    {
        try
        {
            var invoice = await db.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

            if (invoice == null)
            {
                return new UpdateInvoiceResult(false, "Faktura nie została znaleziona.");
            }

            // Aktualizacja nagłówka
            invoice.InvoiceNumber = request.InvoiceNumber;
            invoice.Date = request.Date;
            invoice.ContractorId = request.ContractorId;

            // Synchronizacja pozycji
            var requestedItemIds = request.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToList();
            
            // 1. Usuwanie pozycji których nie ma w żądaniu
            var itemsToRemove = invoice.Items.Where(i => !requestedItemIds.Contains(i.Id)).ToList();
            foreach (var item in itemsToRemove)
            {
                invoice.Items.Remove(item);
            }

            // 2. Aktualizacja i dodawanie pozycji
            foreach (var itemDto in request.Items)
            {
                if (itemDto.Id.HasValue)
                {
                    // Aktualizacja istniejącej
                    var existingItem = invoice.Items.FirstOrDefault(i => i.Id == itemDto.Id.Value);
                    if (existingItem != null)
                    {
                        existingItem.Name = itemDto.Name;
                        existingItem.Quantity = itemDto.Quantity;
                        existingItem.UnitPrice = itemDto.UnitPrice;
                        existingItem.TotalPrice = itemDto.Quantity * itemDto.UnitPrice;
                    }
                }
                else
                {
                    // Dodanie nowej
                    invoice.Items.Add(new InvoiceItem
                    {
                        Name = itemDto.Name,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        TotalPrice = itemDto.Quantity * itemDto.UnitPrice
                    });
                }
            }

            await db.SaveChangesAsync(ct);

            return new UpdateInvoiceResult(true);
        }
        catch (Exception ex)
        {
            return new UpdateInvoiceResult(false, ex.Message);
        }
    }
}
