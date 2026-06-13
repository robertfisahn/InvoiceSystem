using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.TestKsefConnection;

public sealed class TestKsefConnectionCommandHandler(AppDbContext dbContext, IKsefClient ksefClient) 
    : IRequestHandler<TestKsefConnectionCommand, TestKsefConnectionResult>
{
    public async Task<TestKsefConnectionResult> Handle(TestKsefConnectionCommand request, CancellationToken cancellationToken)
    {
        string nip = request.Nip;
        string apiKey = request.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var setting = await dbContext.KsefSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            if (setting == null || string.IsNullOrWhiteSpace(setting.ApiKey))
            {
                return new TestKsefConnectionResult(false, "Brak zapisanej konfiguracji KSeF. Wprowadź NIP oraz Token przed uruchomieniem testu.");
            }
            nip = setting.Nip;
            apiKey = setting.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(nip))
        {
            return new TestKsefConnectionResult(false, "Wprowadź NIP firmy.");
        }

        try
        {
            // 1. Get Challenge
            var challenge = await ksefClient.AuthorisationChallengeAsync(nip, cancellationToken);

            // 2. Try initializing session
            var sessionToken = await ksefClient.InitSessionAsync(
                nip,
                apiKey,
                challenge.Challenge,
                challenge.Timestamp,
                cancellationToken
            );

            // 3. Clean up / close test session
            await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);

            return new TestKsefConnectionResult(true, "Połączenie z Sandboxem KSeF nawiązane pomyślnie!");
        }
        catch (Exception ex)
        {
            return new TestKsefConnectionResult(false, $"Błąd połączenia: {ex.Message}");
        }
    }
}
