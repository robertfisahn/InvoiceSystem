using System;
using System.Globalization;
using System.Linq;
using System.Text;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;

namespace InvoiceSystem.Web.Modules.Ksef.Infrastructure;

public static class KsefXmlSerializer
{
    private record ParsedAddress(string AdresL1, string? AdresL2);

    private static ParsedAddress ParseContractorAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new ParsedAddress("ul. Nieznana 1", "00-000 Nieznane");
        }

        var cleanAddress = address.Replace("\r", "").Replace("\n", ", ").Trim();
        var match = System.Text.RegularExpressions.Regex.Match(cleanAddress, @"\b\d{2}-\d{3}\b");
        if (!match.Success)
        {
            return new ParsedAddress(cleanAddress, null);
        }

        var postCode = match.Value;
        var index = match.Index;

        var street = cleanAddress.Substring(0, index).Trim().TrimEnd(',').Trim();
        var city = cleanAddress.Substring(index + postCode.Length).Trim().TrimStart(',').Trim();

        if (string.IsNullOrEmpty(street))
        {
            street = cleanAddress;
            return new ParsedAddress(street, null);
        }

        var adresL2 = $"{postCode} {city}".Trim();
        return new ParsedAddress(street, adresL2);
    }

    public static string SerializeToFa3(Invoice invoice, string sellerNip)
    {
        var cleanedSellerNip = new string((sellerNip ?? "0000000000").Where(char.IsDigit).ToArray());
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<Faktura xmlns=\"http://crd.gov.pl/wzor/2025/06/25/13775/\">");
        sb.AppendLine("    <Naglowek>");
        sb.AppendLine("        <KodFormularza kodSystemowy=\"FA (3)\" wersjaSchemy=\"1-0E\">FA</KodFormularza>");
        sb.AppendLine("        <WariantFormularza>3</WariantFormularza>");
        sb.AppendLine($"        <DataWytworzeniaFa>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</DataWytworzeniaFa>");
        sb.AppendLine("    </Naglowek>");
        
        // Podmiot1 - Sprzedawca (My)
        sb.AppendLine("    <Podmiot1>");
        sb.AppendLine("        <DaneIdentyfikacyjne>");
        sb.AppendLine($"            <NIP>{cleanedSellerNip}</NIP>");
        sb.AppendLine("            <Nazwa>InvoiceSystem Enterprise</Nazwa>");
        sb.AppendLine("        </DaneIdentyfikacyjne>");
        sb.AppendLine("        <Adres>");
        sb.AppendLine("            <KodKraju>PL</KodKraju>");
        sb.AppendLine("            <AdresL1>ul. Technologiczna 12</AdresL1>");
        sb.AppendLine("            <AdresL2>80-001 Gdańsk</AdresL2>");
        sb.AppendLine("        </Adres>");
        sb.AppendLine("    </Podmiot1>");
 
        // Podmiot2 - Nabywca (Kontrahent)
        var contractorNipRaw = invoice.Contractor?.TaxId ?? "0000000000";
        var contractorNip = new string(contractorNipRaw.Where(char.IsDigit).ToArray());
        var contractorName = invoice.Contractor?.Name ?? "Kontrahent Nieznany";
        sb.AppendLine("    <Podmiot2>");
        sb.AppendLine("        <DaneIdentyfikacyjne>");
        sb.AppendLine($"            <NIP>{contractorNip}</NIP>");
        sb.AppendLine($"            <Nazwa>{contractorName}</Nazwa>");
        sb.AppendLine("        </DaneIdentyfikacyjne>");
        if (invoice.Contractor != null && !string.IsNullOrWhiteSpace(invoice.Contractor.Address))
        {
            var parsedAddr = ParseContractorAddress(invoice.Contractor.Address);
            sb.AppendLine("        <Adres>");
            sb.AppendLine("            <KodKraju>PL</KodKraju>");
            sb.AppendLine($"            <AdresL1>{parsedAddr.AdresL1}</AdresL1>");
            if (!string.IsNullOrEmpty(parsedAddr.AdresL2))
            {
                sb.AppendLine($"            <AdresL2>{parsedAddr.AdresL2}</AdresL2>");
            }
            sb.AppendLine("        </Adres>");
        }
        sb.AppendLine("        <JST>2</JST>");
        sb.AppendLine("        <GV>2</GV>");
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

        sb.AppendLine($"        <P_13_1>{netSum.ToString("F2", CultureInfo.InvariantCulture)}</P_13_1>");
        sb.AppendLine($"        <P_14_1>{vatSum.ToString("F2", CultureInfo.InvariantCulture)}</P_14_1>");
        sb.AppendLine($"        <P_15>{grossSum.ToString("F2", CultureInfo.InvariantCulture)}</P_15>");

        sb.AppendLine("        <Adnotacje>");
        sb.AppendLine("            <P_16>2</P_16>");
        sb.AppendLine("            <P_17>2</P_17>");
        sb.AppendLine("            <P_18>2</P_18>");
        sb.AppendLine("            <P_18A>2</P_18A>");
        sb.AppendLine("            <Zwolnienie>");
        sb.AppendLine("                <P_19N>1</P_19N>");
        sb.AppendLine("            </Zwolnienie>");
        sb.AppendLine("            <NoweSrodkiTransportu>");
        sb.AppendLine("                <P_22N>1</P_22N>");
        sb.AppendLine("            </NoweSrodkiTransportu>");
        sb.AppendLine("            <P_23>2</P_23>");
        sb.AppendLine("            <PMarzy>");
        sb.AppendLine("                <P_PMarzyN>1</P_PMarzyN>");
        sb.AppendLine("            </PMarzy>");
        sb.AppendLine("        </Adnotacje>");
        sb.AppendLine("        <RodzajFaktury>VAT</RodzajFaktury>");

        // Wiersze faktury
        int lineNo = 1;
        foreach (var item in invoice.Items)
        {
            var lineNet = item.Quantity * item.UnitPrice;
            sb.AppendLine("        <FaWiersz>");
            sb.AppendLine($"            <NrWierszaFa>{lineNo++}</NrWierszaFa>");
            sb.AppendLine($"            <P_7>{item.Name}</P_7>");
            sb.AppendLine("            <P_8A>szt</P_8A>");
            sb.AppendLine($"            <P_8B>{item.Quantity.ToString("G", CultureInfo.InvariantCulture)}</P_8B>");
            sb.AppendLine($"            <P_9A>{item.UnitPrice.ToString("F2", CultureInfo.InvariantCulture)}</P_9A>");
            sb.AppendLine($"            <P_11>{lineNet.ToString("F2", CultureInfo.InvariantCulture)}</P_11>");
            sb.AppendLine("            <P_12>23</P_12>"); // Stawka 23%
            sb.AppendLine("        </FaWiersz>");
        }

        sb.AppendLine("    </Fa>");
        sb.AppendLine("</Faktura>");

        return sb.ToString();
    }
}
