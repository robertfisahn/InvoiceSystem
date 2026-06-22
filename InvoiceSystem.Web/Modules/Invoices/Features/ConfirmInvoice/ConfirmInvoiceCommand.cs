using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.ConfirmInvoice;

public record ConfirmInvoiceCommand(int Id) : IRequest<Unit>;
