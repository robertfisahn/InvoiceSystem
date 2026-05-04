using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

public record GetInvoiceForUpdateQuery(int Id) : IRequest<UpdateInvoiceViewModel?>;
