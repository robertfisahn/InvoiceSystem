using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

public sealed class KsefClient(HttpClient httpClient) : IKsefClient
{
    private readonly HttpClient _httpClient = httpClient;
    private const string SandboxUrl = "https://api-test.ksef.mf.gov.pl/v2";

    public async Task<KsefChallengeResult> AuthorisationChallengeAsync(string nip, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            contextIdentifier = new
            {
                type = "onip",
                identifier = nip
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/auth/Challenge")
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
        // Encrypt token + challenge timestamp with public key
        var encryptedToken = KsefCryptography.EncryptToken(apiKey, timestamp);

        var requestBody = new
        {
            challenge = challenge,
            contextIdentifier = new
            {
                type = "Nip",
                value = nip
            },
            encryptedToken = encryptedToken
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/auth/ksef-token")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        
        if (doc.RootElement.TryGetProperty("authenticationToken", out var authTokenElement))
        {
            var token = authTokenElement.GetProperty("token").GetString() ?? string.Empty;
            var referenceNumber = doc.RootElement.TryGetProperty("referenceNumber", out var refProp) 
                ? refProp.GetString() 
                : string.Empty;

            if (!string.IsNullOrEmpty(referenceNumber) && !string.IsNullOrEmpty(token))
            {
                // Poll authentication status for KSeF v2 to ensure the session is active before returning
                var authenticated = false;
                var retries = 20; // 20 retries * 500ms = 10s max
                while (retries > 0 && !authenticated)
                {
                    await Task.Delay(500, cancellationToken);
                    var statusRequest = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/auth/{referenceNumber}");
                    statusRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    var statusResponse = await _httpClient.SendAsync(statusRequest, cancellationToken);
                    if (statusResponse.IsSuccessStatusCode)
                    {
                        var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
                        try
                        {
                            System.IO.File.WriteAllText("c:\\Users\\rober\\.gemini\\antigravity\\scratch\\InvoiceSystem\\scratch\\auth_status.txt", statusJson);
                        }
                        catch {}
                        using var statusDoc = JsonDocument.Parse(statusJson);
                        if (statusDoc.RootElement.TryGetProperty("status", out var statusElement))
                        {
                            var code = statusElement.GetProperty("code").GetInt32();
                            var description = statusElement.TryGetProperty("description", out var descElement) 
                                ? descElement.GetString() 
                                : string.Empty;

                            if (code == 200)
                            {
                                authenticated = true;
                            }
                            else if (code != 100)
                            {
                                throw new InvalidOperationException($"KSeF Authentication failed with code {code}: {description}");
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"KSeF status check failed with HTTP {statusResponse.StatusCode}");
                    }
                    retries--;
                }

                if (!authenticated)
                {
                    throw new TimeoutException("KSeF Session initialization timed out.");
                }

                // Exchange the RefreshToken (token) for the actual AccessToken (ContextToken) using /auth/token/redeem
                var refreshRequest = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/auth/token/redeem");
                refreshRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var refreshResponse = await _httpClient.SendAsync(refreshRequest, cancellationToken);
                refreshResponse.EnsureSuccessStatusCode();

                var refreshJson = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
                using var refreshDoc = JsonDocument.Parse(refreshJson);
                var accessToken = refreshDoc.RootElement.GetProperty("accessToken").GetProperty("token").GetString() ?? string.Empty;

                return $"{accessToken}|{token}";
            }

            return token;
        }

        throw new InvalidOperationException("Could not extract authentication token from KSeF response.");
    }

    public async Task CloseSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{SandboxUrl}/auth/sessions/current");
        SetSessionAuthHeader(request, sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void SetSessionAuthHeader(HttpRequestMessage request, string sessionToken)
    {
        var tokenToUse = sessionToken;
        if (sessionToken.Contains('|'))
        {
            tokenToUse = sessionToken.Split('|')[0];
        }

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
    }

    public async Task<string> SendInvoiceAsync(string sessionToken, string invoiceXml, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Invoice/Send");
        SetSessionAuthHeader(request, sessionToken);
        request.Content = new StringContent(invoiceXml, Encoding.UTF8, "application/xml");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("transactionId").GetString() ?? string.Empty;
    }

    public async Task<KsefStatusResult> GetInvoiceStatusAsync(string sessionToken, string transactionId, CancellationToken cancellationToken = default)
    {
        var referenceNumber = KsefCryptography.ExtractReferenceNumberFromJwt(sessionToken) ?? string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/sessions/{referenceNumber}/invoices/{transactionId}");
        SetSessionAuthHeader(request, sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new KsefStatusResult("Failed", null, $"HTTP Error {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        
        var statusElement = doc.RootElement.GetProperty("status");
        var code = statusElement.GetProperty("code").GetInt32();
        var description = statusElement.GetProperty("description").GetString() ?? "Unknown";

        var status = "Unknown";
        if (code == 200) status = "Processed";
        else if (code == 100 || code == 150) status = "Processing";
        else if (code >= 400) status = "Failed";

        string? ksefNumber = null;
        if (doc.RootElement.TryGetProperty("ksefNumber", out var numProp))
        {
            ksefNumber = numProp.GetString();
        }

        return new KsefStatusResult(status, ksefNumber, code >= 400 ? description : null);
    }

    public async Task<List<KsefIncomingInvoiceDto>> SyncInvoicesAsync(string sessionToken, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            subjectType = "Subject2", // incoming cost invoices where taxpayer is the Buyer
            dateRange = new
            {
                dateType = "PermanentStorage",
                from = fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                to = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK")
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/invoices/query/metadata?sortOrder=Asc&pageOffset=0&pageSize=100")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        SetSessionAuthHeader(request, sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var invoices = new List<KsefIncomingInvoiceDto>();

        if (doc.RootElement.TryGetProperty("invoices", out var invoicesList))
        {
            foreach (var element in invoicesList.EnumerateArray())
            {
                var ksefNumber = element.GetProperty("ksefNumber").GetString() ?? string.Empty;
                
                var sellerElement = element.GetProperty("seller");
                var sellerNip = sellerElement.GetProperty("nip").GetString() ?? string.Empty;
                var sellerName = sellerElement.TryGetProperty("name", out var nameProp) 
                    ? nameProp.GetString() ?? string.Empty 
                    : string.Empty;
                
                var issueDate = element.GetProperty("issueDate").GetDateTime();
                var amount = element.GetProperty("grossAmount").GetDecimal();

                invoices.Add(new KsefIncomingInvoiceDto(ksefNumber, sellerName, sellerNip, issueDate, amount, string.Empty));
            }
        }

        return invoices;
    }

    public async Task<string> DownloadInvoiceXmlAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default)
    {
        var retries = 3;
        while (true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/invoices/ksef/{ksefNumber}");
            SetSessionAuthHeader(request, sessionToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if ((int)response.StatusCode == 429 && retries > 0)
            {
                retries--;
                var delayMs = 2000;
                if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Delta.HasValue)
                {
                    delayMs = (int)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds;
                }
                await Task.Delay(delayMs, cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }

    public async Task<string> DownloadUpoAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default)
    {
        var referenceNumber = KsefCryptography.ExtractReferenceNumberFromJwt(sessionToken) ?? string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/sessions/{referenceNumber}/invoices/ksef/{ksefNumber}/upo");
        SetSessionAuthHeader(request, sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
