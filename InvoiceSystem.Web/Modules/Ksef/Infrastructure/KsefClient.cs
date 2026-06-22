using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Web.Modules.Ksef.Infrastructure;

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

public sealed class KsefClient(HttpClient httpClient, ILogger<KsefClient> logger) : IKsefClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<KsefClient> _logger = logger;
    private const string SandboxUrl = "https://api-test.ksef.mf.gov.pl/v2";

    private async Task EnsureSuccessResponseAsync(HttpResponseMessage response, string actionName, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("exception", out var exceptionProp))
            {
                var serviceCode = exceptionProp.TryGetProperty("serviceCode", out var codeProp) ? codeProp.GetString() : "";
                var serviceName = exceptionProp.TryGetProperty("serviceName", out var nameProp) ? nameProp.GetString() : "";
                var serviceCtx = exceptionProp.TryGetProperty("serviceCtx", out var ctxProp) ? ctxProp.GetString() : "";

                if (!string.IsNullOrEmpty(serviceCode) || !string.IsNullOrEmpty(serviceCtx))
                {
                    throw new KsefApiException(serviceCode ?? "", serviceName ?? actionName, serviceCtx ?? "Nieznany błąd", content);
                }
            }
        }
        catch (JsonException)
        {
            // Not valid JSON, fall back to standard HTTP error
        }

        throw new HttpRequestException($"Błąd KSeF ({actionName}): HTTP {(int)response.StatusCode} {response.ReasonPhrase}. Szczegóły: {content}");
    }

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
        await EnsureSuccessResponseAsync(response, "AuthorisationChallenge", cancellationToken);

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
        await EnsureSuccessResponseAsync(response, "InitSession", cancellationToken);

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
                    await EnsureSuccessResponseAsync(statusResponse, "CheckSessionStatus", cancellationToken);

                    var statusJson = await statusResponse.Content.ReadAsStringAsync(cancellationToken);
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
                await EnsureSuccessResponseAsync(refreshResponse, "RedeemToken", cancellationToken);

                var refreshJson = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
                using var refreshDoc = JsonDocument.Parse(refreshJson);
                var accessToken = refreshDoc.RootElement.GetProperty("accessToken").GetProperty("token").GetString() ?? string.Empty;

                // Open interactive online session
                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();
                var aesKey = aes.Key;
                var iv = aes.IV;

                var encryptedKey = KsefCryptography.EncryptSymmetricKey(aesKey);

                var sessionRequestBody = new
                {
                    formCode = new
                    {
                        systemCode = "FA (3)",
                        schemaVersion = "1-0E",
                        value = "FA"
                    },
                    encryption = new
                    {
                        encryptedSymmetricKey = Convert.ToBase64String(encryptedKey),
                        initializationVector = Convert.ToBase64String(iv),
                        publicKeyId = KsefCryptography.GetPublicKeyId()
                    }
                };

                var sessionRequest = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/sessions/online")
                {
                    Content = new StringContent(JsonSerializer.Serialize(sessionRequestBody), Encoding.UTF8, "application/json")
                };
                sessionRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var sessionResponse = await _httpClient.SendAsync(sessionRequest, cancellationToken);
                await EnsureSuccessResponseAsync(sessionResponse, "OpenSession", cancellationToken);

                var sessionJson = await sessionResponse.Content.ReadAsStringAsync(cancellationToken);
                using var sessionDoc = JsonDocument.Parse(sessionJson);
                var onlineSessionRef = sessionDoc.RootElement.GetProperty("referenceNumber").GetString() ?? string.Empty;

                return $"{accessToken}|{token}|{Convert.ToBase64String(aesKey)}|{Convert.ToBase64String(iv)}|{onlineSessionRef}";
            }

            return token;
        }

        throw new InvalidOperationException("Could not extract authentication token from KSeF response.");
    }

    public async Task CloseSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var parts = sessionToken.Split('|');
        var accessToken = parts[0];

        // 1. Close online session if reference exists
        if (parts.Length >= 5 && !string.IsNullOrEmpty(parts[4]))
        {
            var onlineSessionRef = parts[4];
            try
            {
                var closeRequest = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/sessions/online/{onlineSessionRef}/close");
                closeRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                var closeResponse = await _httpClient.SendAsync(closeRequest, cancellationToken);
                if (!closeResponse.IsSuccessStatusCode)
                {
                    var errorContent = await closeResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Failed to close KSeF online session {SessionRef}. Status: {Status}, Body: {Body}", 
                        onlineSessionRef, closeResponse.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to close KSeF online session {SessionRef}", onlineSessionRef);
            }
        }

        // 2. Terminate auth session
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{SandboxUrl}/auth/sessions/current");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessResponseAsync(response, "CloseSession", cancellationToken);
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

    private record ParsedSessionToken(string AccessToken, byte[]? AesKey, byte[]? Iv, string OnlineSessionRef);

    private ParsedSessionToken ParseSessionToken(string sessionToken)
    {
        var parts = sessionToken.Split('|');
        var accessToken = parts[0];
        
        if (parts.Length >= 5)
        {
            return new ParsedSessionToken(
                accessToken,
                Convert.FromBase64String(parts[2]),
                Convert.FromBase64String(parts[3]),
                parts[4]
            );
        }
        
        return new ParsedSessionToken(accessToken, null, null, string.Empty);
    }

    public async Task<string> SendInvoiceAsync(string sessionToken, string invoiceXml, CancellationToken cancellationToken = default)
    {
        var parsed = ParseSessionToken(sessionToken);
        if (parsed.AesKey == null || parsed.Iv == null || string.IsNullOrEmpty(parsed.OnlineSessionRef))
        {
            throw new InvalidOperationException("Active session is not fully initialized for encryption.");
        }

        var xmlBytes = Encoding.UTF8.GetBytes(invoiceXml);
        var fileSize = xmlBytes.Length;

        // Encrypt invoice xml with AES-256-CBC
        byte[] encryptedXmlBytes;
        using (var aes = Aes.Create())
        {
            aes.Key = parsed.AesKey;
            aes.IV = parsed.Iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var encryptor = aes.CreateEncryptor())
            {
                encryptedXmlBytes = encryptor.TransformFinalBlock(xmlBytes, 0, xmlBytes.Length);
            }
        }

        string hashBase64;
        using (var sha256 = SHA256.Create())
        {
            hashBase64 = Convert.ToBase64String(sha256.ComputeHash(xmlBytes));
        }

        string encryptedHashBase64;
        using (var sha256 = SHA256.Create())
        {
            encryptedHashBase64 = Convert.ToBase64String(sha256.ComputeHash(encryptedXmlBytes));
        }

        var requestBody = new
        {
            invoiceHash = hashBase64,
            invoiceSize = fileSize,
            encryptedInvoiceHash = encryptedHashBase64,
            encryptedInvoiceSize = encryptedXmlBytes.Length,
            encryptedInvoiceContent = Convert.ToBase64String(encryptedXmlBytes)
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/sessions/online/{parsed.OnlineSessionRef}/invoices");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", parsed.AccessToken);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessResponseAsync(response, "SendInvoice", cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("referenceNumber").GetString() ?? string.Empty;
    }

    public async Task<KsefStatusResult> GetInvoiceStatusAsync(string sessionToken, string transactionId, CancellationToken cancellationToken = default)
    {
        var parts = sessionToken.Split('|');
        var refToUse = (parts.Length >= 5 && !string.IsNullOrEmpty(parts[4]))
            ? parts[4]
            : (KsefCryptography.ExtractReferenceNumberFromJwt(sessionToken) ?? string.Empty);

        var actualTransactionId = transactionId;
        if (transactionId.Contains(':'))
        {
            var txParts = transactionId.Split(':');
            refToUse = txParts[0];
            actualTransactionId = txParts[1];
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/sessions/{refToUse}/invoices/{actualTransactionId}");
        SetSessionAuthHeader(request, sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessResponseAsync(response, "CheckInvoiceStatus", cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        
        var statusElement = doc.RootElement.GetProperty("status");
        var code = statusElement.GetProperty("code").GetInt32();
        var description = statusElement.GetProperty("description").GetString() ?? "Unknown";

        var status = "Unknown";
        if (code == 200) status = "Processed";
        else if (code == 100 || code == 150) status = "Processing";
        else if (code >= 400)
        {
            status = "Failed";
            _logger.LogError("KSeF invoice status polling returned failure. Full response JSON: {Json}", json);
        }

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
        await EnsureSuccessResponseAsync(response, "SyncInvoices", cancellationToken);

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
        while (true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/invoices/ksef/{ksefNumber}");
            SetSessionAuthHeader(request, sessionToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(1000, cancellationToken);
                continue;
            }
            await EnsureSuccessResponseAsync(response, "DownloadInvoiceXml", cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }

    public async Task<string> DownloadUpoAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default)
    {
        var parts = sessionToken.Split('|');
        var refToUse = (parts.Length >= 5 && !string.IsNullOrEmpty(parts[4]))
            ? parts[4]
            : (KsefCryptography.ExtractReferenceNumberFromJwt(sessionToken) ?? string.Empty);

        var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/sessions/{refToUse}/invoices/ksef/{ksefNumber}/upo");
        SetSessionAuthHeader(request, sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessResponseAsync(response, "DownloadUpo", cancellationToken);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
