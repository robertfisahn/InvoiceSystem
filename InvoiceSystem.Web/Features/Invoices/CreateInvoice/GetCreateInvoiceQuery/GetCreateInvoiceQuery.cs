using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice.GetCreateInvoiceQuery;

public record GetCreateInvoiceQuery : IRequest<CreateInvoiceViewModel>;
