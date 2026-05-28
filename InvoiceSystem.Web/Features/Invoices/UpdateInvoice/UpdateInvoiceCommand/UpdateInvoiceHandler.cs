using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice.UpdateInvoiceCommand;

public sealed class UpdateInvoiceHandler(AppDbContext db)
    : IRequestHandler<UpdateInvoiceCommand, Unit>
{
    public async Task<Unit> Handle(UpdateInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new InvalidOperationException($"Invoice {request.Id} not found.");

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("Można edytować wyłącznie faktury o statusie Szkic (Draft).");
        }

        // Aktualizacja nagłówka
        invoice.InvoiceNumber = request.InvoiceNumber;
        invoice.Date = request.Date;
        invoice.ContractorId = request.ContractorId;

        // Synchronizacja pozycji
        var requestedItemIds = request.Items
            .Where(i => i.Id.HasValue)
            .Select(i => i.Id!.Value)
            .ToList();

        // 1. Usuwanie pozycji których nie ma w żądaniu
        var itemsToRemove = invoice.Items
            .Where(i => !requestedItemIds.Contains(i.Id))
            .ToList();
        foreach (var item in itemsToRemove)
            invoice.Items.Remove(item);

        // 2. Aktualizacja istniejących i dodawanie nowych
        foreach (var itemDto in request.Items)
        {
            if (itemDto.Id.HasValue)
            {
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
        return Unit.Value;
    }
}
