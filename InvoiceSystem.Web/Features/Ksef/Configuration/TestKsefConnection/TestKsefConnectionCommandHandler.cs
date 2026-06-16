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
        catch (KsefApiException ex)
        {
            if (ex.ServiceName == "AuthorisationChallenge" && (ex.ServiceCode == "21405" || ex.ServiceCtx.Contains("NIP", StringComparison.OrdinalIgnoreCase)))
            {
                return new TestKsefConnectionResult(false, $"Błąd walidacji KSeF: Niepoprawny NIP. Szczegóły: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "InitSession" && (ex.ServiceCode == "21111" || ex.ServiceCtx.Contains("token", StringComparison.OrdinalIgnoreCase) || ex.ServiceCtx.Contains("uprawni", StringComparison.OrdinalIgnoreCase)))
            {
                return new TestKsefConnectionResult(false, $"Błąd autoryzacji KSeF: Niepoprawny token API lub brak uprawnień. Szczegóły: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "CheckSessionStatus" && ex.ServiceCode == "21304")
            {
                return new TestKsefConnectionResult(false, $"Błąd KSeF: Nieprawidłowy numer referencyjny sesji. Szczegóły: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "RedeemToken")
            {
                if (ex.ServiceCode == "21301")
                {
                    return new TestKsefConnectionResult(false, $"Błąd autoryzacji tokenu KSeF: Niepoprawny token sesji lub brak uprawnień. Szczegóły: {ex.ServiceCtx}");
                }
                if (ex.ServiceCode == "21304")
                {
                    return new TestKsefConnectionResult(false, $"Błąd tokenu KSeF: Nieprawidłowy identyfikator sesji. Szczegóły: {ex.ServiceCtx}");
                }
                if (ex.ServiceCode == "21308")
                {
                    return new TestKsefConnectionResult(false, $"Błąd sesji KSeF: Sesja została już zakończona. Szczegóły: {ex.ServiceCtx}");
                }
                return new TestKsefConnectionResult(false, $"Błąd wymiany tokenu KSeF: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "OpenSession")
            {
                if (ex.ServiceCode == "21470")
                {
                    return new TestKsefConnectionResult(false, $"Błąd otwarcia sesji KSeF: Błąd uwierzytelniania sesji interaktywnej. Szczegóły: {ex.ServiceCtx}");
                }
                return new TestKsefConnectionResult(false, $"Błąd otwarcia sesji KSeF: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "CloseSession")
            {
                return new TestKsefConnectionResult(false, $"Błąd zamykania sesji KSeF: {ex.ServiceCtx}");
            }
            return new TestKsefConnectionResult(false, $"Błąd KSeF [{ex.ServiceCode}] w usłudze '{ex.ServiceName}': {ex.ServiceCtx}");
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return new TestKsefConnectionResult(false, "Przekroczono limit żądań do KSeF (429). Spróbuj ponownie później.");
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
        {
            return new TestKsefConnectionResult(false, "Sesja KSeF wygasła (410 Gone). Proszę spróbować ponownie.");
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            return new TestKsefConnectionResult(false, $"Błąd komunikacji z KSeF: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new TestKsefConnectionResult(false, $"Błąd połączenia: {ex.Message}");
        }
    }
}
