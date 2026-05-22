using System;
using System.Collections.Generic;
using InvoiceSystem.Web.Domain.Entities;

namespace InvoiceSystem.Web.Features.Dashboard;

public sealed class DashboardViewModel
{
    public decimal TotalAmount { get; set; }
    public int TotalCount { get; set; }

    public decimal PaidAmount { get; set; }
    public int PaidCount { get; set; }

    public decimal ConfirmedAmount { get; set; }
    public int ConfirmedCount { get; set; }

    public decimal DraftAmount { get; set; }
    public int DraftCount { get; set; }

    public double PaidRatio { get; set; }

    public List<DashboardRecentInvoiceDto> RecentInvoices { get; set; } = [];
}

public sealed class DashboardRecentInvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ContractorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; }
}
