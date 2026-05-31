using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice.GetCreateInvoiceQuery;

public sealed class GetCreateInvoiceHandler(AppDbContext db)
    : IRequestHandler<GetCreateInvoiceQuery, CreateInvoiceViewModel>
{
    public async Task<CreateInvoiceViewModel> Handle(GetCreateInvoiceQuery request, CancellationToken ct)
    {
        var contractors = await db.Contractors
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ContractorLookupDto(c.Id, c.Name))
            .ToListAsync(ct);

        return new CreateInvoiceViewModel { Contractors = contractors };
    }
}
