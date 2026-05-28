using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.TestKsefConnection;

public sealed class TestKsefConnectionCommandHandler(IKsefClient ksefClient) 
    : IRequestHandler<TestKsefConnectionCommand, TestKsefConnectionResult>
{
    public async Task<TestKsefConnectionResult> Handle(TestKsefConnectionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nip) || string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return new TestKsefConnectionResult(false, "Wprowadź NIP oraz Token Autoryzacyjny.");
        }

        try
        {
            // 1. Get Challenge
            var challenge = await ksefClient.AuthorisationChallengeAsync(request.Nip, cancellationToken);

            // 2. Try initializing session
            var sessionToken = await ksefClient.InitSessionAsync(
                request.Nip,
                request.ApiKey,
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
