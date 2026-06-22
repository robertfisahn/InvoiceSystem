using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace InvoiceSystem.Web.Modules.Ksef.Infrastructure;

public static class KsefCryptography
{
    // Test environment token encryption certificate (Base64 X.509 format for Sandbox)
    private const string KsefTokenEncryptionCertBase64 = 
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

    // Test environment symmetric key encryption certificate (Base64 X.509 format for Sandbox)
    private const string KsefSymmetricKeyEncryptionCertBase64 = 
        "MIIGWDCCBECgAwIBAgIQe4NvS/i3yyDCcnaXiiC6BTANBgkqhkiG9w0BAQsFADBOMQswCQYDVQQGEwJQ" +
        "TDEhMB8GA1UECgwYQXNzZWNvIERhdGEgU3lzdGVtcyBTLkEuMRwwGgYDVQQDDBNDZXJ0dW0gU01JTUUg" +
        "UlNBIENBMB4XDTI1MDkyOTA2MTc0NVoXDTI3MDkyOTA2MTc0NFowgb4xGTAXBgNVBGETEFZBVFBMLTUy" +
        "NjAyNTAyNzQxCzAJBgNVBAYTAlBMMRQwEgYDVQQIDAttYXpvd2llY2tpZTERMA8GA1UEBwwIV2Fyc3ph" +
        "d2ExHzAdBgNVBAoMFk1pbmlzdGVyc3R3byBGaW5hbnPDs3cxHzAdBgNVBAMMFk1pbmlzdGVyc3R3byBG" +
        "aW5hbnPDs3cxKTAnBgkqhkiG9w0BCQEWGmtvbnN1bHRhY2plLmtzZWZAbWYuZ292LnBsMIIBIjANBgkq" +
        "hkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAxUEDI48g+Pk0izn9XydRevJqtz4h8s4Sz63FvIZvmhdaZfVk" +
        "mGBqQrBKPTFX6ksQM/gEq1y8nqtmSI6RqoMUgV0UDqIPauyicMiKsfLLPH3ht8bjkeaMB330dxWKCTpJ" +
        "Kv+6+LC73i3B1oavWMAAv3is5aWTyyFB9rwjdxcSZ46DSKYaUo5KbWKZTxBNpCT/LqkhHxfbszq+LIW" +
        "Ivm+09GFpth6hBvDST1h7CHt4g9B1DmtY3I2nYDkPtnmvGo5XBODqTgzWMb0rLgloQGbIeZygQPhhzsW" +
        "Dy4d2uIrE9zZB90q6kDOVg/hZ5YdhCr4X8FeHOfaCgGp+8ZPL3akduQIDAQABo4IBvzCCAbswDAYDVR" +
        "0TAQH/BAIwADBBBgNVHR8EOjA4MDagNKAyhjBodHRwOi8vY3NtaW1lcnNhY2EuY3JsLmNlcnR1bS5wb" +
        "C9jc21pbWVyc2FjYS5jcmwwgYMGCCsGAQUFBwEBBHcwdTAuBggrBgEFBQcwAYYiaHR0cDovL2NzbWlt" +
        "ZXJzYWNhLm9jc3AtY2VydHVtLmNvbTBDBggrBgEFBQcwAoY3aHR0cDovL2NzbWltZXJzYWNhLnJlcG9" +
        "zaXRvcnkuY2VydHVtLnBsL2NzbWltZXJzYWNhLmNlcjAfBgNVHSMEGDAWgBRm+8MPvvS/4JzJq03eRx" +
        "m9wMqmaDAdBgNVHQ4EFgQUkfHAvyAeN71BQRh6DhAThyZqK+kwTAYDVR0gBEUwQzAJBgdngQwBBQICMD" +
        "YGCyqEaAGG9ncCZAMBMCcwJQYIKwYBBQUHAgEWGWh0dHBzOi8vd3d3LmNlcnR1bS5wbC9DUFMwHQYDVR" +
        "0lBBYwFAYIKwYBBQUHAwQGCCsGAQUFBwMCMA4GA1UdDwEB/wQEAwIE8DAlBgNVHREEHjAcgRprb25zd" +
        "Wx0YWNqZS5rc2VmQG1mLmdvdi5wbDANBgkqhkiG9w0BAQsFAAOCAgEAmb9Ck+p9INBVNBAOBkogqtMA" +
        "iCNukI/PXzZiIZaEztNuBn0e/LwHqtFS3MzoOuIxlSM81PaxMRwj/RIhnkghacW0ugdYOY7cH6EjNIG" +
        "kOox2RfnDi11ve/O6JBq0YIrg68SfHSJZxF8FDNr1le2FYe/5TftC2MlXP/4GAgu6UpOdY7DoAEXeLw" +
        "ZYew8FiPhJ48gSPShzJkKP6FQoe9BotSbwpDfrfnNiKeKowr4y5Ru+jwhioKxRN00EoRAQ8glqigJjM" +
        "/Z+qc3SzW4wzVMHGAnO3sKD9phwwHVFQsL/LjQxbs/s8ZD7R8hCbgkyJVnbRcKW3LWKkMUrgZ9Hh+cU" +
        "Xjun6TJ0eiz7713h4515/nGA5fWmu26tWp0kWus1K9JW6YPAEptxdME1gtV8Y+Eo4SHa4M7hdq7sgpu" +
        "mUwI9K3NjWQVEVqYYyNnh8Er1mJloskMFRK63H85DL02zc/WbiMilqRi+bbAaieOQyjB25NToM1LH4N" +
        "4UBbjZBnK0bueaIsV+DGP298Uz4SW3xCliU9oxo3a587WWq4zRhXReeq/wkgXPcx9rz7WTLZ81h3ApH" +
        "95BVH6RJAYGHcmmlEjyKkhv0zpjTH/CiYO4IlWQn9JlEV9UVD8QeJ195Qod7N10LHG7Yv7Zobt7byzd" +
        "423Cou0G/UFLvSxXRImD/cKUyhA=";

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

        var certBytes = Convert.FromBase64String(KsefTokenEncryptionCertBase64);
        using var cert = X509CertificateLoader.LoadCertificate(certBytes);
        using var rsa = cert.GetRSAPublicKey();
        
        if (rsa == null) throw new InvalidOperationException("Could not extract RSA public key from KSeF certificate.");

        // Encrypt with RSA-OAEP SHA256 padding
        var encryptedBytes = rsa.Encrypt(clearBytes, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static byte[] EncryptSymmetricKey(byte[] keyBytes)
    {
        var certBytes = Convert.FromBase64String(KsefSymmetricKeyEncryptionCertBase64);
        using var cert = X509CertificateLoader.LoadCertificate(certBytes);
        using var rsa = cert.GetRSAPublicKey();
        if (rsa == null) throw new InvalidOperationException("Could not extract RSA public key from KSeF certificate.");
        return rsa.Encrypt(keyBytes, RSAEncryptionPadding.OaepSHA256);
    }

    public static string GetPublicKeyId()
    {
        var certBytes = Convert.FromBase64String(KsefSymmetricKeyEncryptionCertBase64);
        using var cert = X509CertificateLoader.LoadCertificate(certBytes);
        using var rsa = cert.GetRSAPublicKey();
        if (rsa == null) throw new InvalidOperationException("Could not extract RSA public key from KSeF certificate.");
        var derBytes = rsa.ExportSubjectPublicKeyInfo();
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(derBytes);
        return Convert.ToBase64String(hash);
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
