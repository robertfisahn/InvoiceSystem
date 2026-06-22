using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

using InvoiceSystem.Web.Modules.Contractors.Domain;

namespace InvoiceSystem.Web.Modules.Contractors.Features.GetContractorList;

public sealed class GetContractorListHandler(AppDbContext db) 
    : IRequestHandler<GetContractorListQuery, List<ContractorDto>>
{
    public async Task<List<ContractorDto>> Handle(GetContractorListQuery request, CancellationToken cancellationToken)
    {
        return await db.Contractors
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ContractorDto(
                c.Id, 
                c.Name, 
                c.TaxId ?? string.Empty, 
                c.Address ?? string.Empty,
                c.SoapVerificationLogs.OrderByDescending(l => l.Timestamp).Select(l => l.VerificationCode).FirstOrDefault(),
                c.SoapVerificationLogs.OrderByDescending(l => l.Timestamp).Select(l => (DateTime?)l.Timestamp).FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);
    }
}
