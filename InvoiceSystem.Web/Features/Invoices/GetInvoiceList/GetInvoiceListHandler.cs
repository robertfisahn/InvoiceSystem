using InvoiceSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceList;

public class GetInvoiceListHandler(AppDbContext context) 
    : IRequestHandler<GetInvoiceListQuery, List<GetInvoiceListViewModel>>
{
    public async Task<List<GetInvoiceListViewModel>> Handle(GetInvoiceListQuery request, CancellationToken cancellationToken)
    {
        return await context.Invoices
            .Select(i => new GetInvoiceListViewModel(
                i.Id,
                i.InvoiceNumber,
                i.Contractor.Name,
                i.Date,
                i.Items.Sum(x => x.TotalPrice)
            ))
            .ToListAsync(cancellationToken);
    }
}
