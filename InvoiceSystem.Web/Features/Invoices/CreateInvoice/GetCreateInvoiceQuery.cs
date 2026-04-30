using InvoiceSystem.Infrastructure.Persistence;
using InvoiceSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

public record GetCreateInvoiceQuery : IRequest<CreateInvoiceViewModel>;

public record CreateInvoiceViewModel
{
    public List<ContractorLookupDto> Contractors { get; init; } = [];
    public CreateInvoiceCommand Command { get; init; } = new();
}

public record ContractorLookupDto(int Id, string Name);

public class GetCreateInvoiceHandler(AppDbContext db) 
    : IRequestHandler<GetCreateInvoiceQuery, CreateInvoiceViewModel>
{
    public async Task<CreateInvoiceViewModel> Handle(GetCreateInvoiceQuery request, CancellationToken ct)
    {
        var contractors = await db.Contractors
            .OrderBy(c => c.Name)
            .Select(c => new ContractorLookupDto(c.Id, c.Name))
            .ToListAsync(ct);

        return new CreateInvoiceViewModel { Contractors = contractors };
    }
}
