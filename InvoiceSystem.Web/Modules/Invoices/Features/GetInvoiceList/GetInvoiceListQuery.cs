using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceList;

public record GetInvoiceListQuery : IRequest<List<GetInvoiceListViewModel>>;

public record GetInvoiceListViewModel(
    int Id,
    string InvoiceNumber,
    string ContractorName,
    DateTime Date,
    decimal TotalAmount,
    InvoiceStatus Status,
    string? KsefNumber
);
