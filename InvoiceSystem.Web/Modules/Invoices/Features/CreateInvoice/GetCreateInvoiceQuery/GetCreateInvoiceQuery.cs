using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.CreateInvoice.GetCreateInvoiceQuery;

public record GetCreateInvoiceQuery : IRequest<CreateInvoiceViewModel>;
