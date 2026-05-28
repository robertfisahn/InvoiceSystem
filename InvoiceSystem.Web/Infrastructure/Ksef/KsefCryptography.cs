using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace InvoiceSystem.Web.Infrastructure.Ksef;

public static class KsefCryptography
{
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

    public static string EncryptToken(string token, string timestamp)
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
        var encryptedBytes = rsa.Encrypt(clearBytes, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string? ExtractReferenceNumberFromJwt(string jwtToken)
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
}
