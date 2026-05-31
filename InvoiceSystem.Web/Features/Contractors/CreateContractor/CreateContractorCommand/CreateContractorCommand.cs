using MediatR;

namespace InvoiceSystem.Web.Features.Contractors.CreateContractor.CreateContractorCommand;

public sealed record CreateContractorCommand : IRequest<CreateContractorResult>
{
    public string? SessionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TaxId { get; init; } = string.Empty;
    public string? Address { get; init; }
}

public sealed record CreateContractorResult(
    bool Success,
    int ContractorId,
    string? ErrorMessage,
    string? CreateInvoiceCommandJson
);
