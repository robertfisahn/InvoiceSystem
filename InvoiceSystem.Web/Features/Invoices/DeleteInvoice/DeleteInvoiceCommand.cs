using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.DeleteInvoice;

public record DeleteInvoiceCommand(int Id) : IRequest<Unit>;
