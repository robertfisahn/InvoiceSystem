using InvoiceSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

public class GetInvoiceForUpdateHandler(AppDbContext db) 
    : IRequestHandler<GetInvoiceForUpdateQuery, UpdateInvoiceViewModel?>
{
    public async Task<UpdateInvoiceViewModel?> Handle(GetInvoiceForUpdateQuery request, CancellationToken ct)
    {
        var invoiceDto = await db.Invoices
            .Where(i => i.Id == request.Id)
            .Select(i => new
            {
                i.Id,
                i.ContractorId,
                i.InvoiceNumber,
                i.Date,
                Items = i.Items.Select(item => new UpdateInvoiceItemCommand
                {
                    Id = item.Id,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (invoiceDto == null) return null;

        var contractors = await db.Contractors
            .OrderBy(c => c.Name)
            .Select(c => new ContractorLookupDto(c.Id, c.Name))
            .ToListAsync(ct);

        return new UpdateInvoiceViewModel
        {
            Id = invoiceDto.Id,
            Contractors = contractors,
            Command = new UpdateInvoiceCommand
            {
                Id = invoiceDto.Id,
                ContractorId = invoiceDto.ContractorId,
                InvoiceNumber = invoiceDto.InvoiceNumber,
                Date = invoiceDto.Date,
                Items = invoiceDto.Items
            }
        };
    }
}
