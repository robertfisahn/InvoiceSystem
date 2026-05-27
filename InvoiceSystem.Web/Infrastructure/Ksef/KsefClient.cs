using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    private const string SandboxUrl = "https://api-test.ksef.mf.gov.pl/v2";
    
    // Test environment public key certificate (Base64 X.509 format for Sandbox)
    private const string KsefSandboxCertificateBase64 = 
        "MIIGWDCCBECgAwIBAgIQGmXqNRg5ye1JMZDOQ7HNCTANBgkqhkiG9w0BAQsFADBOMQswCQYDVQQGEwJQ" +
        "TDEhMB8GA1UECgwYQXNzZWNvIERhdGEgU3lzdGVtcyBTLkEuMRwwGgYDVQQDDBNDZXJ0dW0gU01JTUUg" +
        "UlNBIENBMB4XDTI1MDkyOTA2MDMxOVoXDTI3MDkyOTA2MDMxOFowgb4xGTAXBgNVBGETEFZBVFBMLTUy" +
        "NjAyNTAyNzQxCzAJBgNVBAYTAlBMMRQwEgYDVQQIDAttYXpvd2llY2tpZTERMA8GA1UEBwwIV2Fyc3ph" +
        "d2ExHzAdBgNVBAoMFk1pbmlzdGVyc3R3byBGaW5hbnPDs3cxHzAdBgNVBAMMFk1pbmlzdGVyc3R3byBG" +
        "aW5hbnPDs3cxKTAnBgkqhkiG9w0BCQEWGmtvbnN1bHRhY2plLmtzZWZAbWYuZ292LnBsMIIBIjANBgkq" +
        "hkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxsyeiYiWB2+KFxEpQGoNQa6W8Pc4kWGl8V+sBMdW3Fqh0lhK" +
        "iqKfpH5RWLDmZ30EzkKJ5+IdaWYFoijhYxDBIBhINVQKlBZvEVd6CfPJUJypa94eRO5cc6IPNI35aMhf" +
        "KP/Kc4A/OiT2J4nyCz6BV98xOXCAlyDPD73XM6O2ormL6gUb673zvjOIakf39tAPPVgWIDuX7GDZYGeb" +
        "N7LXoGvjPo5YDqC2KN51ofLbO+n74iei5OaGN94Ap52vI7uzK2g/hQslOd0Avl2U1kwRnnF0yzwbDzRr" +
        "HqPCHUYxVp5nHdo+jHe1CNoa6gt0m6pn1StYcitSXKg2hTNjnes6TQIDAQABo4IBvzCCAbswDAYDVR0T" +
        "AQH/BAIwADBBBgNVHR8EOjA4MDagNKAyhjBodHRwOi8vY3NtaW1lcnNhY2EuY3JsLmNlcnR1bS5wbC9j" +
        "c21pbWVyc2FjYS5jcmwwgYMGCCsGAQUFBwEBBHcwdTAuBggrBgEFBQcwAYYiaHR0cDovL2NzbWltZXJz" +
        "YWNhLm9jc3AtY2VydHVtLmNvbTBDBggrBgEFBQcwAoY3aHR0cDovL2NzbWltZXJzYWNhLnJlcG9zaXRv" +
        "cnkuY2VydHVtLnBsL2NzbWltZXJzYWNhLmNlcjAfBgNVHSMEGDAWgBRm+8MPvvS/4JzJq03eRxm9wMqm" +
        "aDAdBgNVHQ4EFgQUyDshAguoI0vLLGyA3aRgapjC4JEwTAYDVR0gBEUwQzAJBgdngQwBBQICMDYGCyqE" +
        "aAGG9ncCZAMBMCcwJQYIKwYBBQUHAgEWGWh0dHBzOi8vd3d3LmNlcnR1bS5wbC9DUFMwHQYDVR0lBBYw" +
        "FAYIKwYBBQUHAwQGCCsGAQUFBwMCMA4GA1UdDwEB/wQEAwIE8DAlBgNVHREEHjAcgRprb25zdWx0YWNq" +
        "ZS5rc2VmQG1mLmdvdi5wbDANBgkqhkiG9w0BAQsFAAOCAgEAxX7ltOEd6+RbztKIgfmpfxsgmg3TXdmw" +
        "Qucy+tw6aqBNF7Xk22PhVcWVgHKLq6xkaCTCfHbfpl6iGWsWkM5re2FltEF8QuLJbI7n6sC/T/pG+aIj" +
        "4TWgaiKO79dST4kda9GxMEuKxZDkC7OXg4optdxB8Kg3ctFPqzLdnH71lL8I+Wo+KIwGe2h0tDMo39+U" +
        "QC2XOd5l//1abiuO8ZMal+NEbz8WBeS4saH3qPcYmB8+4hV16kU4csNcyrR6PBKO7vkUXI0Lqh0ioEyF" +
        "Jyhxmx3ZPN4VUQFyQZ8l+GmbRFWBCHIhB5dfWmGazE1gWQmVfpYsmuot7sSI2Uw1pBLPsniA9sBQfIB1" +
        "tPGmdfTb/Cpkj1k/owYN6G+08dTqG7v8O7R+skSgcem4O9Ftr+8RDTmhLrPwpx9RXf881bmm47aw1BTP" +
        "zzqDsFGmcNF2Hjb8OpJoJ3ZQQ98ep+yJUb0Ub9trfQNRVKfWltrTDl4Uc+vlWYegocIhJdZ5ZwpeJIZM" +
        "UHIUXarsuC/hyHZb5nDZSp/mf8a1Qxw9SeYb5TG6AAacGfnVPK2YXA6mu4thhszUBxc6/WKEovN8LfYh" +
        "Zzppf9bgp1Gs7TJIzPUJbsAD2tZg2VssAsuJ17u10vTBEZnYwRZXsfMnMJJMC1wsrA5Xmp3C+oN84Z8l" +
        "7viL5l1VxbU=";

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
        if (IsMockMode(nip, apiKey))
        {
            return "mock-session-token-" + Guid.NewGuid().ToString("N");
        }

        // 1. Encrypt token + challenge timestamp with public key
        var encryptedToken = EncryptToken(apiKey, timestamp);

        HttpRequestMessage request;
        if (SandboxUrl.Contains("v2"))
        {
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

            request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/auth/ksef-token")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
        }
        else
        {
            // 2. Generate XML InitSessionTokenRequest (fallback for v1)
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

            request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Session/InitToken")
            {
                Content = new StringContent(xmlRequest, Encoding.UTF8, "application/octet-stream")
            };
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        
        // Support both KSeF v2 (authenticationToken) and KSeF v1 (sessionToken)
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

                // Return compound token to store both ContextToken (first) and RefreshToken (second)
                return $"{accessToken}|{token}";
            }

            return token;
        }
        else if (doc.RootElement.TryGetProperty("sessionToken", out var sessionTokenElement))
        {
            return sessionTokenElement.GetProperty("token").GetString() ?? string.Empty;
        }

        throw new InvalidOperationException("Could not extract authentication token from KSeF response.");
    }

    public async Task CloseSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        if (sessionToken.StartsWith("mock-session"))
        {
            return;
        }

        HttpRequestMessage request;
        if (SandboxUrl.Contains("v2"))
        {
            request = new HttpRequestMessage(HttpMethod.Delete, $"{SandboxUrl}/auth/sessions/current");
        }
        else
        {
            request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Session/Close");
        }

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

        if (SandboxUrl.Contains("v2"))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
        }
        else
        {
            request.Headers.Add("Session-Token", tokenToUse);
        }
    }

    private static string? ExtractReferenceNumberFromJwt(string jwtToken)
    {
        try
        {
            var tokenToDecode = jwtToken;
            if (jwtToken.Contains('|'))
            {
                tokenToDecode = jwtToken.Split('|')[1];
            }

            var parts = tokenToDecode.Split('.');
            if (parts.Length < 2) return null;

            var payloadBase64 = parts[1];
            // Normalize base64url padding
            payloadBase64 = payloadBase64.Replace('-', '+').Replace('_', '/');
            switch (payloadBase64.Length % 4)
            {
                case 2: payloadBase64 += "=="; break;
                case 3: payloadBase64 += "="; break;
            }

            var payloadBytes = Convert.FromBase64String(payloadBase64);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("arn", out var arnProp))
            {
                return arnProp.GetString();
            }
            if (doc.RootElement.TryGetProperty("operation-reference-number", out var refProp))
            {
                return refProp.GetString();
            }
        }
        catch
        {
            // Ignore decoding errors
        }
        return null;
    }

    public async Task<string> SendInvoiceAsync(string sessionToken, string invoiceXml, CancellationToken cancellationToken = default)
    {
        if (sessionToken.StartsWith("mock-session"))
        {
            return "mock-transaction-" + Guid.NewGuid().ToString("N");
        }

        // KSeF expects zipped/base64 or direct request with session headers
        var request = new HttpRequestMessage(HttpMethod.Post, $"{SandboxUrl}/online/Invoice/Send");
        SetSessionAuthHeader(request, sessionToken);

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

        var referenceNumber = ExtractReferenceNumberFromJwt(sessionToken) ?? string.Empty;
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

        // Query incoming invoices in KSeF v2 format
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

        // KSeF v2 uses /invoices/query/metadata with paginated query params
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
        if (sessionToken.StartsWith("mock-session"))
        {
            var buyerNip = sessionToken.Replace("mock-session-", "");
            if (string.IsNullOrEmpty(buyerNip) || buyerNip == "mock-session")
            {
                buyerNip = "1111111111";
            }
            var buyerName = buyerNip == "2222222222" ? "Moja Firma Sp. z o.o." : "InvoiceSystem Enterprise";

            var nip = ksefNumber.Split('-')[0];
            string sellerName = "Google Poland Sp. z o.o.";
            string invNum = "G-998/05/2026";
            decimal amount = 3658.54m; // 4500 gross

            if (nip == "5270103391")
            {
                sellerName = "Microsoft Sp. z o.o.";
                invNum = "FV/2026/05/100";
                amount = 1000.00m; // 1230 gross
            }
            else if (nip == "5541453879")
            {
                sellerName = "COMED - Dorota XXXXXXXX";
                invNum = "COM/2026/05/88";
                amount = 100.00m; // 123 gross
            }
            else if (nip == "1111111111")
            {
                if (ksefNumber.Contains("7BBD41C00000"))
                {
                    sellerName = "GSMM Sp. z o.o.";
                    invNum = "GSM/2026/05/99";
                    amount = 206.48m; // 253.97 gross
                }
                else
                {
                    sellerName = "SELLER Sp. z o.o.";
                    invNum = "SEL/2026/05/01";
                    amount = 100.00m;
                }
            }

            return GetMockInvoiceXml(sellerName, nip, invNum, amount, buyerName, buyerNip);
        }

        var retries = 3;
        while (true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/invoices/ksef/{ksefNumber}");
            SetSessionAuthHeader(request, sessionToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if ((int)response.StatusCode == 429 && retries > 0)
            {
                retries--;
                var delayMs = 2000; // wait 2 seconds by default
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

        var referenceNumber = ExtractReferenceNumberFromJwt(sessionToken) ?? string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"{SandboxUrl}/sessions/{referenceNumber}/invoices/ksef/{ksefNumber}/upo");
        SetSessionAuthHeader(request, sessionToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private string EncryptToken(string token, string timestamp)
    {
        // Convert timestamp (ISO 8601 or numeric string) to Unix timestamp in milliseconds (ms since epoch)
        long timestampMs;
        if (long.TryParse(timestamp, out var parsedMs))
        {
            timestampMs = parsedMs;
        }
        else if (DateTimeOffset.TryParse(timestamp, out var parsedDateTime))
        {
            timestampMs = parsedDateTime.ToUnixTimeMilliseconds();
        }
        else
        {
            throw new ArgumentException("Invalid timestamp format. Must be an ISO 8601 string or Unix timestamp in milliseconds.", nameof(timestamp));
        }

        // Concatenate token and challenge timestamp in ms
        var clearText = $"{token}|{timestampMs}";
        var clearBytes = Encoding.UTF8.GetBytes(clearText);

        var certBytes = Convert.FromBase64String(KsefSandboxCertificateBase64);
        using var cert = X509CertificateLoader.LoadCertificate(certBytes);
        using var rsa = cert.GetRSAPublicKey();
        
        if (rsa == null) throw new InvalidOperationException("Could not extract RSA public key from KSeF certificate.");

        // Encrypt with RSA-OAEP SHA256 padding
        var encryptedBytes = rsa.Encrypt(clearBytes, System.Security.Cryptography.RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encryptedBytes);
    }

    private string GetMockInvoiceXml(string name, string nip, string invoiceNumber, decimal amount, string buyerName = "InvoiceSystem Enterprise", string buyerNip = "1111111111")
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
            <NIP>{buyerNip}</NIP>
            <Nazwa>{buyerName}</Nazwa>
        </DanePodmiotu>
        <Adres>
            <AdresPol>
                <KodPocztowy>02-300</KodPocztowy>
                <Miejscowosc>Rzeszów</Miejscowosc>
                <Ulica>ul. Technologiczna 12</Ulica>
            </AdresPol>
        </Adres>
    </Podmiot2>
    <Fa>
        <KodWaluty>PLN</KodWaluty>
        <P_1>{DateTime.UtcNow.AddDays(-2):yyyy-MM-dd}</P_1>
        <P_2>{invoiceNumber}</P_2>
        <P_13_1>{amount:F2}</P_13_1>
        <P_14_1>{(amount * 0.23m):F2}</P_14_1>
        <P_15>{(amount * 1.23m):F2}</P_15>
        <FaWiersz>
            <P_7>Usługa IT / Wsparcie techniczne</P_7>
            <P_8A>1</P_8A>
            <P_9B>{amount:F2}</P_9B>
            <P_11>{amount:F2}</P_11>
        </FaWiersz>
    </Fa>
</Faktura>";
    }
}
