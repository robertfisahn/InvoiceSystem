using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceList;

public sealed class GetInvoiceListHandler(AppDbContext context)
    : IRequestHandler<GetInvoiceListQuery, List<GetInvoiceListViewModel>>
{
    public async Task<List<GetInvoiceListViewModel>> Handle(GetInvoiceListQuery request, CancellationToken cancellationToken)
    {
        return await context.Invoices
            .AsNoTracking()
            .Select(i => new GetInvoiceListViewModel(
                i.Id,
                i.InvoiceNumber,
                i.Contractor.Name,
                i.Date,
                i.Items.Sum(x => x.TotalPrice),
                i.Status,
                i.KsefNumber
            ))
            .ToListAsync(cancellationToken);
    }
}
