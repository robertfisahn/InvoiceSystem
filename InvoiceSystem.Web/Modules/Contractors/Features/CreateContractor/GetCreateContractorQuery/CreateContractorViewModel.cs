using InvoiceSystem.Web.Modules.Contractors.Features.CreateContractor.CreateContractorCommand;

namespace InvoiceSystem.Web.Modules.Contractors.Features.CreateContractor.GetCreateContractorQuery;

public sealed class CreateContractorViewModel
{
    public string? SessionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TaxId { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? WarningMessage { get; init; }

    public static CreateContractorViewModel From(CreateContractorCommand.CreateContractorCommand command, string? warningMessage = null)
    {
        return new CreateContractorViewModel
        {
            SessionId = command.SessionId,
            Name = command.Name ?? string.Empty,
            TaxId = command.TaxId ?? string.Empty,
            Address = command.Address,
            WarningMessage = warningMessage
        };
    }
}
