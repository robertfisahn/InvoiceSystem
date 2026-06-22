using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceDetails;

public record GetInvoiceDetailsQuery(int Id) : IRequest<GetInvoiceDetailsViewModel?>;
