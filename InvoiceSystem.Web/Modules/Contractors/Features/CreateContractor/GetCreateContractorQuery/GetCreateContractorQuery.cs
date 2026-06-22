using MediatR;

namespace InvoiceSystem.Web.Modules.Contractors.Features.CreateContractor.GetCreateContractorQuery;

public sealed record GetCreateContractorQuery(string? SessionId) : IRequest<CreateContractorViewModel>;
