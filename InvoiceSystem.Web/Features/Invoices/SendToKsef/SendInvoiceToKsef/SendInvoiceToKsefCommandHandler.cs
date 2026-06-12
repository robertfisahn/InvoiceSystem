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
        if (setting == null || !setting.IsEnabled || string.IsNullOrWhiteSpace(setting.Nip) || string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            return new SendInvoiceToKsefResult(false, null, null, "Integracja z KSeF jest wyłączona lub nieskonfigurowana. Skonfiguruj NIP i Token w ustawieniach.");
        }

        try
        {
            // 1. Generate XML
            var xmlContent = KsefXmlSerializer.SerializeToFa2(invoice, setting.Nip);

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
            
            invoice.KsefTransactionId = transactionId;
            invoice.KsefSentAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            // 4. Check status (fast processing mock response)
            var statusResult = await ksefClient.GetInvoiceStatusAsync(sessionToken, transactionId, cancellationToken);
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
        catch (Exception ex)
        {
            return new SendInvoiceToKsefResult(false, null, null, ex.Message);
        }
    }
}
