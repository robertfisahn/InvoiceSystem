using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Invoices.Features.Import;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Modules.Contractors.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Modules.Contractors.Features.CreateContractor.CreateContractorCommand;

public sealed class CreateContractorCommandHandler(
    AppDbContext db,
    IMemoryCache cache,
    ILogger<CreateContractorCommandHandler> logger
) : IRequestHandler<CreateContractorCommand, CreateContractorResult>
{
    public async Task<CreateContractorResult> Handle(CreateContractorCommand request, CancellationToken cancellationToken)
    {
        var cleanTaxId = CleanTaxId(request.TaxId);

        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Sprawdzamy czy kontrahent o tym NIP nie istnieje już w bazie
            var contractor = await db.Contractors
                .FirstOrDefaultAsync(c => c.TaxId == cleanTaxId, cancellationToken);

            if (contractor is null)
            {
                contractor = new Contractor
                {
                    Name = request.Name.Trim(),
                    TaxId = cleanTaxId,
                    Address = string.IsNullOrWhiteSpace(request.Address) ? "Brak adresu" : request.Address.Trim()
                };
                db.Contractors.Add(contractor);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Zarejestrowano nowego kontrahenta: {Name} (NIP: {Nip})", contractor.Name, contractor.TaxId);
            }

            await transaction.CommitAsync(cancellationToken);

            string? createInvoiceCommandJson = null;

            // Jeśli komenda przyszła z sesji OCR, przygotowujemy dane dla autopilota faktury
            if (!string.IsNullOrWhiteSpace(request.SessionId))
            {
                var cacheKey = $"ocr-session-{request.SessionId}";
                if (cache.TryGetValue<OcrSessionData>(cacheKey, out var sessionData) && sessionData is not null)
                {
                    var createCommand = new InvoiceSystem.Web.Modules.Invoices.Features.CreateInvoice.CreateInvoiceCommand.CreateInvoiceCommand
                    {
                        ContractorId = contractor.Id,
                        Date = sessionData.Date,
                        FilePath = sessionData.FilePath,
                        Items = sessionData.Items.Select(i => new InvoiceSystem.Web.Modules.Invoices.Features.CreateInvoice.CreateInvoiceCommand.CreateInvoiceItemCommand(
                            i.Name, i.Quantity, i.UnitPrice)).ToList()
                    };

                    createInvoiceCommandJson = JsonSerializer.Serialize(createCommand);
                    cache.Remove(cacheKey); // Usuwamy dane z cache, proces zakończony sukcesem!
                }
            }

            return new CreateContractorResult(
                Success: true,
                ContractorId: contractor.Id,
                ErrorMessage: null,
                CreateInvoiceCommandJson: createInvoiceCommandJson
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Błąd podczas zapisu rejestracji nowego kontrahenta w CreateContractorCommandHandler.");
            return new CreateContractorResult(
                Success: false,
                ContractorId: 0,
                ErrorMessage: $"Błąd zapisu w bazie danych: {ex.Message}",
                CreateInvoiceCommandJson: null
            );
        }
    }

    private string CleanTaxId(string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId)) return string.Empty;
        var sb = new System.Text.StringBuilder();
        foreach (var c in taxId)
        {
            if (char.IsDigit(c)) sb.Append(c);
        }
        return sb.ToString();
    }
}
