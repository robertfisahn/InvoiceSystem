using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceDetails;

public sealed class GetInvoiceDetailsHandler(AppDbContext db)
    : IRequestHandler<GetInvoiceDetailsQuery, GetInvoiceDetailsViewModel?>
{
    public async Task<GetInvoiceDetailsViewModel?> Handle(GetInvoiceDetailsQuery request, CancellationToken ct)
    {
        return await db.Invoices
            .AsNoTracking()
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
                i.Status,
                i.KsefNumber,
                i.KsefTransactionId,
                i.KsefSentAt,
                i.UpoXml
            ))
            .FirstOrDefaultAsync(ct);
    }
}
