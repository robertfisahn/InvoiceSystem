using System;

namespace InvoiceSystem.Web.Infrastructure.Ksef;

public sealed class KsefApiException : Exception
{
    public string ServiceCode { get; }
    public string ServiceName { get; }
    public string ServiceCtx { get; }
    public string RawResponse { get; }

    public KsefApiException(string serviceCode, string serviceName, string serviceCtx, string rawResponse)
        : base($"Błąd KSeF [{serviceCode}] w usłudze '{serviceName}': {serviceCtx}")
    {
        ServiceCode = serviceCode;
        ServiceName = serviceName;
        ServiceCtx = serviceCtx;
        RawResponse = rawResponse;
    }
}
