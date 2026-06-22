using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Modules.Dashboard.Features.Dashboard;

public sealed class GetDashboardHandler(AppDbContext db)
    : IRequestHandler<GetDashboardQuery, DashboardViewModel>
{
    public async Task<DashboardViewModel> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var invoices = await db.Invoices
            .AsNoTracking()
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                ContractorName = i.Contractor.Name,
                i.Date,
                TotalAmount = i.Items.Sum(item => item.TotalPrice),
                i.Status
            })
            .ToListAsync(cancellationToken);

        var totalAmount = invoices.Sum(x => x.TotalAmount);
        var totalCount = invoices.Count;

        var paidInvoices = invoices.Where(x => x.Status == InvoiceStatus.Paid).ToList();
        var paidAmount = paidInvoices.Sum(x => x.TotalAmount);
        var paidCount = paidInvoices.Count;

        var confirmedInvoices = invoices.Where(x => x.Status == InvoiceStatus.Confirmed).ToList();
        var confirmedAmount = confirmedInvoices.Sum(x => x.TotalAmount);
        var confirmedCount = confirmedInvoices.Count;

        var draftInvoices = invoices.Where(x => x.Status == InvoiceStatus.Draft).ToList();
        var draftAmount = draftInvoices.Sum(x => x.TotalAmount);
        var draftCount = draftInvoices.Count;

        var paidRatio = totalAmount > 0 ? (double)(paidAmount / totalAmount) * 100 : 0.0;

        var recentInvoices = invoices
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Take(5)
            .Select(x => new DashboardRecentInvoiceDto
            {
                Id = x.Id,
                InvoiceNumber = x.InvoiceNumber,
                ContractorName = x.ContractorName,
                Date = x.Date,
                TotalAmount = x.TotalAmount,
                Status = x.Status
            })
            .ToList();

        return new DashboardViewModel
        {
            TotalAmount = totalAmount,
            TotalCount = totalCount,
            PaidAmount = paidAmount,
            PaidCount = paidCount,
            ConfirmedAmount = confirmedAmount,
            ConfirmedCount = confirmedCount,
            DraftAmount = draftAmount,
            DraftCount = draftCount,
            PaidRatio = Math.Round(paidRatio, 1),
            RecentInvoices = recentInvoices
        };
    }
}
