using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.ConfirmInvoice;

public record ConfirmInvoiceCommand(int Id) : IRequest<Unit>;
