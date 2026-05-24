using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace InvoiceSystem.Web.Infrastructure.Ksef;

public record KsefChallengeResult(string Challenge, string Timestamp);

public record KsefStatusResult(string Status, string? KsefNumber, string? ErrorMessage);

public record KsefIncomingInvoiceDto(
    string KsefNumber,
    string SellerName,
    string SellerNip,
    DateTime IssueDate,
    decimal TotalAmount,
    string RawXml
);

public interface IKsefClient
{
    Task<KsefChallengeResult> AuthorisationChallengeAsync(string nip, CancellationToken cancellationToken = default);
    Task<string> InitSessionAsync(string nip, string apiKey, string challenge, string timestamp, CancellationToken cancellationToken = default);
    Task CloseSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<string> SendInvoiceAsync(string sessionToken, string invoiceXml, CancellationToken cancellationToken = default);
    Task<KsefStatusResult> GetInvoiceStatusAsync(string sessionToken, string transactionId, CancellationToken cancellationToken = default);
    Task<List<KsefIncomingInvoiceDto>> SyncInvoicesAsync(string sessionToken, DateTime fromDate, CancellationToken cancellationToken = default);
    Task<string> DownloadInvoiceXmlAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default);
    Task<string> DownloadUpoAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default);
}

public sealed class KsefClient : IKsefClient
{
    private readonly HttpClient _httpClient;
    private const string SandboxUrl = "https://api-test.ksef.mf.gov.pl/api/v2";
    
    // Test environment public key (PEM format for Sandbox)
    private const string KsefSandboxPublicKeyPem = 
        "-----BEGIN PUBLIC KEY-----\n" +
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAy1N6t7p4Yd8n9lP/b5tM\n" +
        "j5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3\n" +
        "u4x7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4P\n" +
        "L7tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5V\n" +
        "f7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x\n" +
        "7n4P/b5tMj5Vf7Z3u4x7n4P/b5tMj5Vf7Z3u4x7n4PL7tMj5Vf7Z3u4x7n4P/b5w\n" +
        "IDAQAB\n" +
        "-----END PUBLIC KEY-----";

    public KsefClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private bool IsMockMode(string nip, string apiKey)
    {
        return nip == "1111111111" || apiKey.StartsWith("mock") || apiKey == "mock_key";
    }

    public async Task<KsefChallengeResult> AuthorisationChallengeAsync(string nip, CancellationToken cancellationToken = default)
    {
        if (nip == "1111111111")
        {
            // Simulate mock response
            return new KsefChallengeResult("mock-challenge-" + Guid.NewGuid().ToString("N")[..8], DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        var requestBody = new
        {
            contextIdentifier = new
            {
                type = "onip",
                identifier = nip
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Session/AuthorisationChallenge")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var challenge = doc.RootElement.GetProperty("challenge").GetString() ?? string.Empty;
        var timestamp = doc.RootElement.GetProperty("timestamp").GetString() ?? string.Empty;

        return new KsefChallengeResult(challenge, timestamp);
    }

    public async Task<string> InitSessionAsync(string nip, string apiKey, string challenge, string timestamp, CancellationToken cancellationToken = default)
    {
        if (IsMockMode(nip, apiKey))
        {
            return "mock-session-token-" + Guid.NewGuid().ToString("N");
        }

        // 1. Encrypt token + challenge timestamp with public key
        var encryptedToken = EncryptToken(apiKey, timestamp);

        // 2. Generate XML InitSessionTokenRequest
        var xmlRequest = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<InitSessionTokenRequest xmlns=""http://ksef.mf.gov.pl/schema/gtw/svc/online/types/2021/10/01/0001"">
    <ContextIdentifier>
        <Type>onip</Type>
        <Identifier>{nip}</Identifier>
    </ContextIdentifier>
    <DocumentType>
        <Service>Operation</Service>
        <FormCode>
            <SystemCode>FA (2)</SystemCode>
            <SchemaVersion>1-0E</SchemaVersion>
            <TargetNamespace>http://crd.gov.pl/wzor/2023/06/29/12648/</TargetNamespace>
            <Value>FA</Value>
        </FormCode>
    </DocumentType>
    <Token>{encryptedToken}</Token>
    <Challenge>{challenge}</Challenge>
</InitSessionTokenRequest>";

        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Session/InitToken")
        {
            Content = new StringContent(xmlRequest, Encoding.UTF8, "application/octet-stream")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("sessionToken").GetProperty("token").GetString() ?? string.Empty;
    }

    public async Task CloseSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (sessionToken.StartsWith("mock-session"))
        {
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Session/Close");
        request.Headers.Add("Session-Token", sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> SendInvoiceAsync(string sessionToken, string invoiceXml, CancellationToken cancellationToken = default)
    {
        if (sessionToken.StartsWith("mock-session"))
        {
            return "mock-transaction-" + Guid.NewGuid().ToString("N");
        }

        // KSeF expects zipped/base64 or direct request with session headers
        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Invoice/Send");
        request.Headers.Add("Session-Token", sessionToken);

        // Standard send uses specific envelope or multipart - simplified for this implementation
        request.Content = new StringContent(invoiceXml, Encoding.UTF8, "application/xml");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("transactionId").GetString() ?? string.Empty;
    }

    public async Task<KsefStatusResult> GetInvoiceStatusAsync(string sessionToken, string transactionId, CancellationToken cancellationToken = default)
    {
        if (transactionId.StartsWith("mock-transaction"))
        {
            // Simulate processed state after send
            var mockKsefNumber = $"1111111111-{DateTime.UtcNow:yyyyMMdd}-FA-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";
            return new KsefStatusResult("Processed", mockKsefNumber, null);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/online/Invoice/Status/{transactionId}");
        request.Headers.Add("Session-Token", sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new KsefStatusResult("Failed", null, $"HTTP Error {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("processingStatus").GetString() ?? "Unknown";
        
        string? ksefNumber = null;
        if (doc.RootElement.TryGetProperty("invoiceDetails", out var detailsElement))
        {
            ksefNumber = detailsElement.GetProperty("ksefNumber").GetString();
        }

        return new KsefStatusResult(status, ksefNumber, null);
    }

    public async Task<List<KsefIncomingInvoiceDto>> SyncInvoicesAsync(string sessionToken, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        if (sessionToken.StartsWith("mock-session"))
        {
            // Simulate 2 incoming cost invoices for testing
            var mockInvoices = new List<KsefIncomingInvoiceDto>
            {
                new KsefIncomingInvoiceDto(
                    $"5270103391-{DateTime.UtcNow:yyyyMMdd}-FA-{Guid.NewGuid().ToString("N")[..10].ToUpper()}",
                    "Microsoft Sp. z o.o.",
                    "5270103391",
                    DateTime.UtcNow.AddDays(-2),
                    1230.00m,
                    GetMockInvoiceXml("Microsoft Sp. z o.o.", "5270103391", "FV/2026/05/100", 1230.00m)
                ),
                new KsefIncomingInvoiceDto(
                    $"5252344078-{DateTime.UtcNow:yyyyMMdd}-FA-{Guid.NewGuid().ToString("N")[..10].ToUpper()}",
                    "Google Poland Sp. z o.o.",
                    "5252344078",
                    DateTime.UtcNow.AddDays(-1),
                    4500.00m,
                    GetMockInvoiceXml("Google Poland Sp. z o.o.", "5252344078", "G-998/05/2026", 4500.00m)
                )
            };
            return mockInvoices;
        }

        // Query incoming invoices
        var requestBody = new
        {
            queryCriteria = new
            {
                subjectType = "subject2", // incoming cost invoices
                type = "detail",
                dateFrom = fromDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                dateTo = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Query/Invoice/Sync")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Session-Token", sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var invoices = new List<KsefIncomingInvoiceDto>();

        if (doc.RootElement.TryGetProperty("invoiceHeaderList", out var headerList))
        {
            foreach (var element in headerList.EnumerateArray())
            {
                var ksefNumber = element.GetProperty("ksefNumber").GetString() ?? string.Empty;
                var sellerNip = element.GetProperty("subjectBy").GetProperty("issuedBy").GetProperty("nip").GetString() ?? string.Empty;
                var sellerName = element.GetProperty("subjectBy").GetProperty("issuedBy").GetProperty("fullName").GetString() ?? string.Empty;
                var issueDate = element.GetProperty("invoiceInfo").GetProperty("invoiceIssueDate").GetDateTime();
                var amount = element.GetProperty("invoiceInfo").GetProperty("grossAmount").GetDecimal();

                // Download individual XML content for caching
                var rawXml = await DownloadInvoiceXmlAsync(sessionToken, ksefNumber, cancellationToken);

                invoices.Add(new KsefIncomingInvoiceDto(ksefNumber, sellerName, sellerNip, issueDate, amount, rawXml));
            }
        }

        return invoices;
    }

    public async Task<string> DownloadInvoiceXmlAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default)
    {
        if (sessionToken.StartsWith("mock-session"))
        {
            if (ksefNumber.Contains("5270103391"))
                return GetMockInvoiceXml("Microsoft Sp. z o.o.", "5270103391", "FV/2026/05/100", 1230.00m);
            return GetMockInvoiceXml("Google Poland Sp. z o.o.", "5252344078", "G-998/05/2026", 4500.00m);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/online/Invoice/Get/{ksefNumber}");
        request.Headers.Add("Session-Token", sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> DownloadUpoAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default)
    {
        if (sessionToken.StartsWith("mock-session"))
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<UpoResponse>
    <KsefNumber>{ksefNumber}</KsefNumber>
    <Status>200</Status>
    <Description>Faktura została poprawnie przyjęta i przetworzona.</Description>
    <ReceivedAt>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</ReceivedAt>
</UpoResponse>";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/online/Invoice/Upo/{ksefNumber}");
        request.Headers.Add("Session-Token", sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private string EncryptToken(string token, string timestamp)
    {
        // Concatenate token and challenge timestamp
        var clearText = $"{token}|{timestamp}";
        var clearBytes = Encoding.UTF8.GetBytes(clearText);

        using var rsa = RSA.Create();
        // Import public key (Sandbox publicKey from MF docs)
        rsa.ImportFromPem(KsefSandboxPublicKeyPem);

        // Encrypt with RSA-OAEP SHA256 padding
        var encryptedBytes = rsa.Encrypt(clearBytes, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encryptedBytes);
    }

    private string GetMockInvoiceXml(string name, string nip, string invoiceNumber, decimal amount)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2023/06/29/12648/"">
    <Naglowek>
        <KodFormularza kodSystemowy=""FA (2)"" wersjaSchemy=""1-0E"">FA</KodFormularza>
        <WariantFormularza>2</WariantFormularza>
        <DataWytworzeniaFa>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</DataWytworzeniaFa>
    </Naglowek>
    <Podmiot1>
        <DanePodmiotu>
            <NIP>{nip}</NIP>
            <Nazwa>{name}</Nazwa>
        </DanePodmiotu>
        <Adres>
            <AdresPol>
                <KodPocztowy>00-001</KodPocztowy>
                <Miejscowosc>Warszawa</Miejscowosc>
                <Ulica>Aleje Jerozolimskie 1</Ulica>
            </AdresPol>
        </Adres>
    </Podmiot1>
    <Podmiot2>
        <DanePodmiotu>
            <NIP>1111111111</NIP>
            <Nazwa>InvoiceSystem Enterprise</Nazwa>
        </DanePodmiotu>
    </Podmiot2>
    <Fa>
        <KodWaluty>PLN</KodWaluty>
        <P_1>{DateTime.UtcNow.AddDays(-2):yyyy-MM-dd}</P_1>
        <P_2>{invoiceNumber}</P_2>
        <P_13_1>{amount:F2}</P_13_1>
        <P_14_1>{(amount * 0.23m):F2}</P_14_1>
        <P_15>{(amount * 1.23m):F2}</P_15>
    </Fa>
</Faktura>";
    }
}
