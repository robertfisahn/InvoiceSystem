using MediatR;

namespace InvoiceSystem.Web.Features.Contractors.CreateContractor.GetCreateContractorQuery;

public sealed record GetCreateContractorQuery(string? SessionId) : IRequest<CreateContractorViewModel>;
