using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceDetails;

public record GetInvoiceDetailsQuery(int Id) : IRequest<GetInvoiceDetailsViewModel?>;
