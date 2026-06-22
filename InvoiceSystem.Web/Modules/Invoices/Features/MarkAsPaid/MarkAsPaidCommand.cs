using MediatR;

namespace InvoiceSystem.Web.Modules.Invoices.Features.MarkAsPaid;

public record MarkAsPaidCommand(int Id) : IRequest<Unit>;
