using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.DeleteInvoice;

public record DeleteInvoiceCommand(int Id) : IRequest<Unit>;
