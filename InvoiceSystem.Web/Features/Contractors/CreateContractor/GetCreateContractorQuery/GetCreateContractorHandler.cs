using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Features.Invoices.Import;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace InvoiceSystem.Web.Features.Contractors.CreateContractor.GetCreateContractorQuery;

public sealed class GetCreateContractorHandler(IMemoryCache cache)
    : IRequestHandler<GetCreateContractorQuery, CreateContractorViewModel>
{
    public Task<CreateContractorViewModel> Handle(GetCreateContractorQuery request, CancellationToken cancellationToken)
    {
        var viewModel = new CreateContractorViewModel();

        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            var cacheKey = $"ocr-session-{request.SessionId}";
            if (cache.TryGetValue<OcrSessionData>(cacheKey, out var sessionData) && sessionData is not null)
            {
                viewModel = new CreateContractorViewModel
                {
                    SessionId = request.SessionId,
                    Name = sessionData.BuyerName ?? string.Empty,
                    TaxId = sessionData.BuyerTaxId ?? string.Empty,
                    Address = sessionData.BuyerAddress,
                    WarningMessage = $"Brak kontrahenta z NIP: {sessionData.BuyerTaxId} w bazie danych. Dane zostały wyodrębnione przez AI. Zweryfikuj i zapisz kontrahenta."
                };
            }
            else
            {
                throw new InvalidOperationException("Sesja analizy OCR wygasła lub jest nieprawidłowa.");
            }
        }

        return Task.FromResult(viewModel);
    }
}
