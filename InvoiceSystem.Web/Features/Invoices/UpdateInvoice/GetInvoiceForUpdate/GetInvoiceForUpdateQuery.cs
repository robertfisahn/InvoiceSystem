using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice.GetInvoiceForUpdate;

public record GetInvoiceForUpdateQuery(int Id) : IRequest<UpdateInvoiceViewModel?>;
