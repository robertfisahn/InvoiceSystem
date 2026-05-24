using InvoiceSystem.Web.Domain.Entities;
using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceList;

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
