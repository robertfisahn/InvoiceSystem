using MediatR;

namespace InvoiceSystem.Web.Features.Invoices.GenerateSample;

public enum SampleInvoiceFormat
{
    Pdf,
    Jpg,
    Png
}

// Komenda do generowania pliku z podanych danych JSON
public record GenerateSampleCommand(string JsonData, SampleInvoiceFormat Format) : IRequest<GenerateSampleResponse>;

public record GenerateSampleResponse(byte[] FileBytes, string ContentType, string FileName);
