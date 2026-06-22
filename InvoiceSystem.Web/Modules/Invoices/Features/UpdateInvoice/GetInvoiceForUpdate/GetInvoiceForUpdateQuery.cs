using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.UpdateInvoice.GetInvoiceForUpdate;

public record GetInvoiceForUpdateQuery(int Id) : IRequest<UpdateInvoiceViewModel?>;
