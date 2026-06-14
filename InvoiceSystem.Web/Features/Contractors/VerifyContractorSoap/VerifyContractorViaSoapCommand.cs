using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Soap;

namespace InvoiceSystem.Web.Features.Contractors.VerifyContractorSoap;

public record VerifyContractorViaSoapCommand(int ContractorId) : IRequest<VerifyContractorViaSoapResult>;

public record VerifyContractorViaSoapResult(
    bool Success,
    string? ErrorMessage,
    string? Name,
    string? Regon,
    string? Address,
    string? StatusVat,
    DateTime CheckedAt
);

public class VerifyContractorViaSoapCommandHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<VerifyContractorViaSoapCommand, VerifyContractorViaSoapResult>
{
    public async Task<VerifyContractorViaSoapResult> Handle(VerifyContractorViaSoapCommand request, CancellationToken cancellationToken)
    {
        var contractor = await dbContext.Contractors
            .FirstOrDefaultAsync(c => c.Id == request.ContractorId, cancellationToken);

        if (contractor == null)
        {
            return new VerifyContractorViaSoapResult(false, "Contractor not found.", null, null, null, null, DateTime.Now);
        }

        if (string.IsNullOrWhiteSpace(contractor.TaxId))
        {
            return new VerifyContractorViaSoapResult(false, "Contractor has no tax identifier (NIP).", null, null, null, null, DateTime.Now);
        }

        // Clean NIP from hyphens/spaces
        string nip = contractor.TaxId.Replace("-", "").Replace(" ", "");

        // Dynamically detect server URL using HttpContext
        var httpContext = httpContextAccessor.HttpContext;
        string soapUrl = "http://localhost:5215/services/regon.asmx";
        if (httpContext != null)
        {
            var req = httpContext.Request;
            soapUrl = $"{req.Scheme}://{req.Host}/services/regon.asmx";
        }

        string apiKey = "abcde12345abcde12345"; // Default public test key

        var binding = new BasicHttpBinding();
        var endpoint = new EndpointAddress(soapUrl);
        var factory = new ChannelFactory<IUslugaBIRzewnPubl>(binding, endpoint);
        var client = factory.CreateChannel();

        string requestXml = $"Zaloguj({apiKey})";
        string responseXml = "";
        string? statusVat = null;
        string? name = null;
        string? regon = null;
        string? address = null;

        try
        {
            // 1. Login
            string sid = await client.Zaloguj(apiKey);
            
            // 2. Search
            using (new OperationContextScope((IContextChannel)client))
            {
                var requestProperty = new System.ServiceModel.Channels.HttpRequestMessageProperty();
                requestProperty.Headers["sid"] = sid;
                OperationContext.Current.OutgoingMessageProperties[System.ServiceModel.Channels.HttpRequestMessageProperty.Name] = requestProperty;

                requestXml = $"DaneSzukajJednostki(NIP: {nip})";
                responseXml = await client.DaneSzukajJednostki(new ParametryWyszukiwania { Nip = nip });
            }

            // 3. Logout
            await client.Wyloguj(sid);

            // Parse responseXml
            var xDoc = XDocument.Parse(responseXml);
            var dane = xDoc.Element("root")?.Element("dane");
            if (dane == null)
            {
                throw new Exception("Invalid XML format returned from search.");
            }

            var errorCode = dane.Element("ErrorCode")?.Value;
            var errorMessagePl = dane.Element("ErrorMessagePl")?.Value;

            if (!string.IsNullOrEmpty(errorCode))
            {
                // Contractor not found in registry (e.g. ErrorCode 4)
                statusVat = "NOT_FOUND";
                
                var notFoundLog = new SoapVerificationLog
                {
                    Id = Guid.NewGuid(),
                    ContractorId = contractor.Id,
                    NipQueried = nip,
                    RequestMethod = "WCF",
                    RequestEnvelope = requestXml,
                    ResponseEnvelope = responseXml,
                    IsValid = false,
                    VerificationCode = "NOT_FOUND",
                    ErrorMessage = errorMessagePl ?? "Nie znaleziono podmiotu dla podanych kryteriów wyszukiwania.",
                    Timestamp = DateTime.UtcNow
                };

                dbContext.SoapVerificationLogs.Add(notFoundLog);
                await dbContext.SaveChangesAsync(cancellationToken);

                return new VerifyContractorViaSoapResult(
                    true,
                    errorMessagePl ?? "Nie znaleziono podmiotu dla podanych kryteriów wyszukiwania.",
                    null,
                    null,
                    null,
                    "NOT_FOUND",
                    notFoundLog.Timestamp
                );
            }

            regon = dane.Element("Regon")?.Value;
            name = dane.Element("Nazwa")?.Value;
            statusVat = dane.Element("StatusVat")?.Value;
            
            string ulica = dane.Element("Ulica")?.Value ?? "";
            string kod = dane.Element("KodPocztowy")?.Value ?? "";
            string miejscowosc = dane.Element("Miejscowosc")?.Value ?? "";
            address = $"{ulica}, {kod} {miejscowosc}".Trim(',', ' ');

            // Create Log
            var log = new SoapVerificationLog
            {
                Id = Guid.NewGuid(),
                ContractorId = contractor.Id,
                NipQueried = nip,
                RequestMethod = "WCF",
                RequestEnvelope = requestXml,
                ResponseEnvelope = responseXml,
                IsValid = statusVat == "ACTIVE",
                VerificationCode = statusVat,
                ErrorMessage = null,
                Timestamp = DateTime.UtcNow
            };

            dbContext.SoapVerificationLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new VerifyContractorViaSoapResult(
                true,
                null,
                name,
                regon,
                address,
                statusVat,
                log.Timestamp
            );
        }
        catch (FaultException ex)
        {
            responseXml = $"FaultException: {ex.Message}";
            
            var log = new SoapVerificationLog
            {
                Id = Guid.NewGuid(),
                ContractorId = contractor.Id,
                NipQueried = nip,
                RequestMethod = "WCF",
                RequestEnvelope = requestXml,
                ResponseEnvelope = responseXml,
                IsValid = false,
                VerificationCode = "ERROR",
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            dbContext.SoapVerificationLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new VerifyContractorViaSoapResult(
                false,
                $"GUS service error: {ex.Message}",
                null,
                null,
                null,
                null,
                log.Timestamp
            );
        }
        catch (Exception ex)
        {
            responseXml = $"Exception: {ex.Message}";

            var log = new SoapVerificationLog
            {
                Id = Guid.NewGuid(),
                ContractorId = contractor.Id,
                NipQueried = nip,
                RequestMethod = "WCF",
                RequestEnvelope = requestXml,
                ResponseEnvelope = responseXml,
                IsValid = false,
                VerificationCode = "ERROR",
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            dbContext.SoapVerificationLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new VerifyContractorViaSoapResult(
                false,
                $"Communication/Parsing error: {ex.Message}",
                null,
                null,
                null,
                null,
                log.Timestamp
            );
        }
        finally
        {
            var channel = client as IClientChannel;
            if (channel != null && channel.State == CommunicationState.Opened)
            {
                try
                {
                    channel.Close();
                }
                catch
                {
                    channel.Abort();
                }
            }
            if (factory.State == CommunicationState.Opened)
            {
                try
                {
                    factory.Close();
                }
                catch
                {
                    factory.Abort();
                }
            }
        }
    }
}
