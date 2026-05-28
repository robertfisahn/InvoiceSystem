using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InvoiceSystem.Web.Features.Invoices.Import.ConfirmOcr;

public sealed class ConfirmOcrInvoiceCommandHandler(
    AppDbContext db,
    IMemoryCache cache
) : IRequestHandler<ConfirmOcrInvoiceCommand, ConfirmOcrInvoiceResult>
{
    public async Task<ConfirmOcrInvoiceResult> Handle(ConfirmOcrInvoiceCommand request, CancellationToken cancellationToken)
    {
        var cacheKey = $"ocr-session-{request.SessionId}";
        if (!cache.TryGetValue<OcrSessionData>(cacheKey, out var sessionData) || sessionData is null)
        {
            return new ConfirmOcrInvoiceResult(
                Success: false,
                ContractorExists: false,
                CreateInvoiceCommandJson: null,
                ErrorMessage: "Sesja analizy OCR wygasła lub jest nieprawidłowa. Prześlij plik ponownie."
            );
        }

        var cleanTaxId = CleanTaxId(request.BuyerTaxId);

        // Aktualizujemy dane sesji zmianami wprowadzonymi przez operatora w formularzu
        sessionData.BuyerName = request.BuyerName;
        sessionData.BuyerTaxId = cleanTaxId;
        sessionData.BuyerAddress = request.BuyerAddress;
        sessionData.Date = request.Date;
        sessionData.Items = request.Items.Select(i => new OcrSessionItem
        {
            Name = i.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        cache.Set(cacheKey, sessionData, TimeSpan.FromMinutes(30));

        // Sprawdzamy czy kontrahent istnieje w bazie
        var contractor = await db.Contractors
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TaxId == cleanTaxId, cancellationToken);

        if (contractor is not null)
        {
            // Kontrahent istnieje - przygotowujemy komendę zapisu faktury i czyścimy sesję
            var createCommand = new CreateInvoice.CreateInvoiceCommand.CreateInvoiceCommand
            {
                ContractorId = contractor.Id,
                Date = sessionData.Date,
                FilePath = sessionData.FilePath,
                Items = sessionData.Items.Select(i => new CreateInvoice.CreateInvoiceCommand.CreateInvoiceItemCommand(
                    i.Name, i.Quantity, i.UnitPrice)).ToList()
            };

            var jsonCommand = JsonSerializer.Serialize(createCommand);
            cache.Remove(cacheKey);

            return new ConfirmOcrInvoiceResult(
                Success: true,
                ContractorExists: true,
                CreateInvoiceCommandJson: jsonCommand,
                ErrorMessage: null
            );
        }
        else
        {
            // Kontrahent nie istnieje - operator musi go najpierw zarejestrować
            return new ConfirmOcrInvoiceResult(
                Success: true,
                ContractorExists: false,
                CreateInvoiceCommandJson: null,
                ErrorMessage: null
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
