using System;
using System.Text;
using InvoiceSystem.Web.Domain.Entities;

namespace InvoiceSystem.Web.Infrastructure.Ksef;

public static class KsefXmlSerializer
{
    public static string SerializeToFa2(Invoice invoice, string sellerNip)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<Faktura xmlns=\"http://crd.gov.pl/wzor/2023/06/29/12648/\">");
        sb.AppendLine("    <Naglowek>");
        sb.AppendLine("        <KodFormularza kodSystemowy=\"FA (2)\" wersjaSchemy=\"1-0E\">FA</KodFormularza>");
        sb.AppendLine("        <WariantFormularza>2</WariantFormularza>");
        sb.AppendLine($"        <DataWytworzeniaFa>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</DataWytworzeniaFa>");
        sb.AppendLine("    </Naglowek>");
        
        // Podmiot1 - Sprzedawca (My)
        sb.AppendLine("    <Podmiot1>");
        sb.AppendLine("        <DanePodmiotu>");
        sb.AppendLine($"            <NIP>{sellerNip}</NIP>");
        sb.AppendLine("            <Nazwa>InvoiceSystem Enterprise</Nazwa>");
        sb.AppendLine("        </DanePodmiotu>");
        sb.AppendLine("        <Adres>");
        sb.AppendLine("            <AdresPol>");
        sb.AppendLine("                <KodPocztowy>80-001</KodPocztowy>");
        sb.AppendLine("                <Miejscowosc>Gdańsk</Miejscowosc>");
        sb.AppendLine("                <Ulica>ul. Technologiczna 12</Ulica>");
        sb.AppendLine("            </AdresPol>");
        sb.AppendLine("        </Adres>");
        sb.AppendLine("    </Podmiot1>");

        // Podmiot2 - Nabywca (Kontrahent)
        var contractorNip = invoice.Contractor?.TaxId ?? "0000000000";
        var contractorName = invoice.Contractor?.Name ?? "Kontrahent Nieznany";
        sb.AppendLine("    <Podmiot2>");
        sb.AppendLine("        <DanePodmiotu>");
        sb.AppendLine($"            <NIP>{contractorNip}</NIP>");
        sb.AppendLine($"            <Nazwa>{contractorName}</Nazwa>");
        sb.AppendLine("        </DanePodmiotu>");
        if (invoice.Contractor != null && !string.IsNullOrEmpty(invoice.Contractor.Address))
        {
            sb.AppendLine("        <Adres>");
            sb.AppendLine("            <AdresPol>");
            sb.AppendLine($"                <Miejscowosc>{invoice.Contractor.Address}</Miejscowosc>");
            sb.AppendLine("            </AdresPol>");
            sb.AppendLine("        </Adres>");
        }
        sb.AppendLine("    </Podmiot2>");

        // Fa - Dane faktury
        sb.AppendLine("    <Fa>");
        sb.AppendLine("        <KodWaluty>PLN</KodWaluty>");
        sb.AppendLine($"        <P_1>{invoice.Date:yyyy-MM-dd}</P_1>");
        sb.AppendLine($"        <P_2>{invoice.InvoiceNumber}</P_2>");

        decimal netSum = 0;
        foreach (var item in invoice.Items)
        {
            netSum += item.Quantity * item.UnitPrice;
        }
        decimal vatSum = netSum * 0.23m; // Standard 23% VAT for our system
        decimal grossSum = netSum + vatSum;

        sb.AppendLine($"        <P_13_1>{netSum:F2}</P_13_1>");
        sb.AppendLine($"        <P_14_1>{vatSum:F2}</P_14_1>");
        sb.AppendLine($"        <P_15>{grossSum:F2}</P_15>");

        // Wiersze faktury
        int lineNo = 1;
        foreach (var item in invoice.Items)
        {
            var lineNet = item.Quantity * item.UnitPrice;
            sb.AppendLine("        <FaWiersz>");
            sb.AppendLine($"            <NrWierszaFa>{lineNo++}</NrWierszaFa>");
            sb.AppendLine($"            <P_7>{item.Name}</P_7>");
            sb.AppendLine($"            <P_8A>{item.Quantity}</P_8A>");
            sb.AppendLine($"            <P_9B>{item.UnitPrice:F2}</P_9B>");
            sb.AppendLine($"            <P_11>{lineNet:F2}</P_11>");
            sb.AppendLine("            <P_12>23</P_12>"); // Stawka 23%
            sb.AppendLine("        </FaWiersz>");
        }

        sb.AppendLine("    </Fa>");
        sb.AppendLine("</Faktura>");

        return sb.ToString();
    }
}
