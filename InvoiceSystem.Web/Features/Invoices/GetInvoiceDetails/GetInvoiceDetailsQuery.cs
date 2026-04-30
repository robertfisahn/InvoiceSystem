using InvoiceSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceDetails;

public record GetInvoiceDetailsQuery(int Id) : IRequest<GetInvoiceDetailsViewModel?>;

public record GetInvoiceDetailsViewModel(
    int Id,
    string InvoiceNumber,
    DateTime Date,
    ContractorDetailsDto Contractor,
    List<InvoiceItemDto> Items,
    decimal TotalNet,
    decimal TotalGross
);

public record ContractorDetailsDto(string Name, string? TaxId, string? Address);
public record InvoiceItemDto(string Name, decimal Quantity, decimal UnitPrice, decimal TotalPrice);

public class GetInvoiceDetailsHandler(AppDbContext db) 
    : IRequestHandler<GetInvoiceDetailsQuery, GetInvoiceDetailsViewModel?>
{
    public async Task<GetInvoiceDetailsViewModel?> Handle(GetInvoiceDetailsQuery request, CancellationToken ct)
    {
        return await db.Invoices
            .Where(i => i.Id == request.Id)
            .Select(i => new GetInvoiceDetailsViewModel(
                i.Id,
                i.InvoiceNumber,
                i.Date,
                new ContractorDetailsDto(i.Contractor.Name, i.Contractor.TaxId, i.Contractor.Address),
                i.Items.Select(item => new InvoiceItemDto(
                    item.Name,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice
                )).ToList(),
                i.Items.Sum(x => x.TotalPrice),
                i.Items.Sum(x => x.TotalPrice)
            ))
            .FirstOrDefaultAsync(ct);
    }
}
