using MediatR;
using System.Collections.Generic;

namespace InvoiceSystem.Web.Features.Contractors.GetContractorList;

public sealed record GetContractorListQuery : IRequest<List<ContractorDto>>;

public sealed record ContractorDto(
    int Id,
    string Name,
    string TaxId,
    string Address,
    string? LatestVatStatus,
    DateTime? LatestVerificationDate);
