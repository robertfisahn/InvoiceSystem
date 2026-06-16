using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Invoices.SendToKsef.SendInvoiceToKsef;

public sealed class SendInvoiceToKsefCommandHandler(AppDbContext dbContext, IKsefClient ksefClient) 
    : IRequestHandler<SendInvoiceToKsefCommand, SendInvoiceToKsefResult>
{
    public async Task<SendInvoiceToKsefResult> Handle(SendInvoiceToKsefCommand request, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Contractor)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice == null)
        {
            return new SendInvoiceToKsefResult(false, null, null, "Faktura nie istnieje.");
        }

        if (invoice.Status != InvoiceStatus.Confirmed)
        {
            return new SendInvoiceToKsefResult(false, null, null, "Tylko zatwierdzone faktury mogą być wysłane do KSeF.");
        }

        if (!string.IsNullOrEmpty(invoice.KsefNumber))
        {
            return new SendInvoiceToKsefResult(false, invoice.KsefNumber, null, "Ta faktura posiada już nadany numer KSeF.");
        }

        var setting = await dbContext.KsefSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            return new SendInvoiceToKsefResult(false, null, null, "Integracja z KSeF nie jest skonfigurowana. Skonfiguruj NIP i Token w ustawieniach.");
        }

        try
        {
            // 1. Generate XML
            var xmlContent = KsefXmlSerializer.SerializeToFa3(invoice, setting.Nip);

            // 2. Authorise Session
            var challenge = await ksefClient.AuthorisationChallengeAsync(setting.Nip, cancellationToken);
            var sessionToken = await ksefClient.InitSessionAsync(
                setting.Nip,
                setting.ApiKey,
                challenge.Challenge,
                challenge.Timestamp,
                cancellationToken
            );

            // 3. Send
            var transactionId = await ksefClient.SendInvoiceAsync(sessionToken, xmlContent, cancellationToken);
            
            var onlineSessionRef = sessionToken.Split('|')[4];
            var combinedTransactionId = $"{onlineSessionRef}:{transactionId}";
            invoice.KsefTransactionId = combinedTransactionId;
            invoice.KsefSentAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            // 4. Check status (fast processing mock response)
            var statusResult = await ksefClient.GetInvoiceStatusAsync(sessionToken, combinedTransactionId, cancellationToken);
            if (statusResult.Status == "Processed" && !string.IsNullOrEmpty(statusResult.KsefNumber))
            {
                invoice.KsefNumber = statusResult.KsefNumber;
                
                // Get UPO
                var upoXml = await ksefClient.DownloadUpoAsync(sessionToken, statusResult.KsefNumber, cancellationToken);
                invoice.UpoXml = upoXml;

                await dbContext.SaveChangesAsync(cancellationToken);
                
                // Close session
                await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);

                return new SendInvoiceToKsefResult(true, statusResult.KsefNumber, transactionId, null);
            }

            // Close session
            await ksefClient.CloseSessionAsync(sessionToken, cancellationToken);

            return new SendInvoiceToKsefResult(true, null, transactionId, null);
        }
        catch (KsefApiException ex)
        {
            if (ex.ServiceName == "AuthorisationChallenge" && (ex.ServiceCode == "21405" || ex.ServiceCtx.Contains("NIP", StringComparison.OrdinalIgnoreCase)))
            {
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd walidacji KSeF: Niepoprawny NIP. Szczegóły: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "InitSession" && (ex.ServiceCode == "21111" || ex.ServiceCtx.Contains("token", StringComparison.OrdinalIgnoreCase) || ex.ServiceCtx.Contains("uprawni", StringComparison.OrdinalIgnoreCase)))
            {
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd autoryzacji KSeF: Niepoprawny token API lub brak uprawnień. Szczegóły: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "CheckSessionStatus" && ex.ServiceCode == "21304")
            {
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd KSeF: Nieprawidłowy numer referencyjny sesji. Szczegóły: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "RedeemToken")
            {
                if (ex.ServiceCode == "21301")
                {
                    return new SendInvoiceToKsefResult(false, null, null, $"Błąd autoryzacji tokenu KSeF: Niepoprawny token sesji lub brak uprawnień. Szczegóły: {ex.ServiceCtx}");
                }
                if (ex.ServiceCode == "21304")
                {
                    return new SendInvoiceToKsefResult(false, null, null, $"Błąd tokenu KSeF: Nieprawidłowy identyfikator sesji. Szczegóły: {ex.ServiceCtx}");
                }
                if (ex.ServiceCode == "21308")
                {
                    return new SendInvoiceToKsefResult(false, null, null, $"Błąd sesji KSeF: Sesja została już zakończona. Szczegóły: {ex.ServiceCtx}");
                }
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd wymiany tokenu KSeF: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "OpenSession")
            {
                if (ex.ServiceCode == "21470")
                {
                    return new SendInvoiceToKsefResult(false, null, null, $"Błąd otwarcia sesji KSeF: Błąd uwierzytelniania sesji interaktywnej. Szczegóły: {ex.ServiceCtx}");
                }
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd otwarcia sesji KSeF: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "SendInvoice")
            {
                if (ex.ServiceCode == "21405")
                {
                    return new SendInvoiceToKsefResult(false, null, null, $"Błąd KSeF: NIP sprzedawcy na fakturze jest niezgodny z NIP-em zalogowanej sesji. Szczegóły: {ex.ServiceCtx}");
                }
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd wysyłki faktury do KSeF: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "CheckInvoiceStatus")
            {
                if (ex.ServiceCode == "21304")
                {
                    return new SendInvoiceToKsefResult(false, null, null, $"Błąd KSeF: Nieprawidłowy identyfikator transakcji podczas sprawdzania statusu wysyłki. Szczegóły: {ex.ServiceCtx}");
                }
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd sprawdzania statusu wysyłki KSeF: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "DownloadUpo")
            {
                if (ex.ServiceCode == "21304")
                {
                    return new SendInvoiceToKsefResult(false, null, null, $"Błąd KSeF: Nieprawidłowy numer KSeF podczas pobierania UPO. Szczegóły: {ex.ServiceCtx}");
                }
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd pobierania UPO z KSeF: {ex.ServiceCtx}");
            }
            if (ex.ServiceName == "CloseSession")
            {
                return new SendInvoiceToKsefResult(false, null, null, $"Błąd zamykania sesji KSeF: {ex.ServiceCtx}");
            }
            return new SendInvoiceToKsefResult(false, null, null, $"Błąd KSeF [{ex.ServiceCode}] w usłudze '{ex.ServiceName}': {ex.ServiceCtx}");
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return new SendInvoiceToKsefResult(false, null, null, "Przekroczono limit żądań do KSeF (429). Spróbuj ponownie później.");
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
        {
            return new SendInvoiceToKsefResult(false, null, null, "Sesja KSeF wygasła (410 Gone). Proszę spróbować ponownie.");
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            return new SendInvoiceToKsefResult(false, null, null, $"Błąd komunikacji z KSeF: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new SendInvoiceToKsefResult(false, null, null, ex.Message);
        }
    }
}
