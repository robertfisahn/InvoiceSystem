using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.MarkAsPaid;

public record MarkAsPaidCommand(int Id) : IRequest<Unit>;
