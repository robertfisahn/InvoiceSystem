using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace InvoiceSystem.Web.Infrastructure.Ksef;

public record ParsedKsefInvoice(
    string InvoiceNumber,
    DateTime Date,
    string SellerName,
    string SellerNip,
    string SellerAddress,
    string BuyerName,
    string BuyerNip,
    string BuyerAddress,
    decimal TotalAmount,
    List<ParsedKsefInvoiceItem> Items
);

public record ParsedKsefInvoiceItem(
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public static class KsefXmlParser
{
    public static ParsedKsefInvoice ParseFa2(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        
        // Find elements ignoring namespaces
        var root = doc.Root;
        if (root == null)
            throw new ArgumentException("Invalid XML document.");

        // Podmiot1 (Sprzedawca)
        var podmiot1 = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Podmiot1");
        string sellerName = string.Empty;
        string sellerNip = string.Empty;
        string sellerAddress = string.Empty;

        if (podmiot1 != null)
        {
            var danePodmiotu = podmiot1.Elements().FirstOrDefault(e => e.Name.LocalName == "DanePodmiotu");
            if (danePodmiotu != null)
            {
                sellerNip = danePodmiotu.Elements().FirstOrDefault(e => e.Name.LocalName == "NIP")?.Value ?? string.Empty;
                sellerName = danePodmiotu.Elements().FirstOrDefault(e => e.Name.LocalName == "Nazwa")?.Value ?? string.Empty;
            }

            var adres = podmiot1.Elements().FirstOrDefault(e => e.Name.LocalName == "Adres");
            if (adres != null)
            {
                var adresPol = adres.Elements().FirstOrDefault(e => e.Name.LocalName == "AdresPol");
                if (adresPol != null)
                {
                    var ulica = adresPol.Elements().FirstOrDefault(e => e.Name.LocalName == "Ulica")?.Value;
                    var nrDomu = adresPol.Elements().FirstOrDefault(e => e.Name.LocalName == "NrDomu")?.Value;
                    var kodPocztowy = adresPol.Elements().FirstOrDefault(e => e.Name.LocalName == "KodPocztowy")?.Value;
                    var miejscowosc = adresPol.Elements().FirstOrDefault(e => e.Name.LocalName == "Miejscowosc")?.Value;
                    
                    var addrParts = new List<string>();
                    if (!string.IsNullOrEmpty(ulica))
                    {
                        addrParts.Add(ulica + (string.IsNullOrEmpty(nrDomu) ? "" : " " + nrDomu));
                    }
                    if (!string.IsNullOrEmpty(kodPocztowy) || !string.IsNullOrEmpty(miejscowosc))
                    {
                        addrParts.Add($"{kodPocztowy} {miejscowosc}".Trim());
                    }
                    sellerAddress = string.Join(", ", addrParts);
                }
                else
                {
                    var adresLnk = adres.Elements().FirstOrDefault(e => e.Name.LocalName == "AdresLnk");
                    if (adresLnk != null)
                    {
                        sellerAddress = adresLnk.Elements().FirstOrDefault(e => e.Name.LocalName == "AdresTekst")?.Value ?? string.Empty;
                    }
                }
            }
        }

        // Podmiot2 (Nabywca)
        var podmiot2 = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Podmiot2");
        string buyerName = string.Empty;
        string buyerNip = string.Empty;
        string buyerAddress = string.Empty;

        if (podmiot2 != null)
        {
            var danePodmiotu = podmiot2.Elements().FirstOrDefault(e => e.Name.LocalName == "DanePodmiotu");
            if (danePodmiotu != null)
            {
                buyerNip = danePodmiotu.Elements().FirstOrDefault(e => e.Name.LocalName == "NIP")?.Value ?? string.Empty;
                buyerName = danePodmiotu.Elements().FirstOrDefault(e => e.Name.LocalName == "Nazwa")?.Value ?? string.Empty;
                
                if (string.IsNullOrEmpty(buyerName))
                {
                    var fizyczna = danePodmiotu.Elements().FirstOrDefault(e => e.Name.LocalName == "OsobaFizyczna");
                    if (fizyczna != null)
                    {
                        var imie = fizyczna.Elements().FirstOrDefault(e => e.Name.LocalName == "ImiePierwsze")?.Value ?? string.Empty;
                        var nazwisko = fizyczna.Elements().FirstOrDefault(e => e.Name.LocalName == "Nazwisko")?.Value ?? string.Empty;
                        buyerName = $"{imie} {nazwisko}".Trim();
                    }
                }
            }

            var adres = podmiot2.Elements().FirstOrDefault(e => e.Name.LocalName == "Adres");
            if (adres != null)
            {
                var adresPol = adres.Elements().FirstOrDefault(e => e.Name.LocalName == "AdresPol");
                if (adresPol != null)
                {
                    var ulica = adresPol.Elements().FirstOrDefault(e => e.Name.LocalName == "Ulica")?.Value;
                    var nrDomu = adresPol.Elements().FirstOrDefault(e => e.Name.LocalName == "NrDomu")?.Value;
                    var kodPocztowy = adresPol.Elements().FirstOrDefault(e => e.Name.LocalName == "KodPocztowy")?.Value;
                    var miejscowosc = adresPol.Elements().FirstOrDefault(e => e.Name.LocalName == "Miejscowosc")?.Value;
                    
                    var addrParts = new List<string>();
                    if (!string.IsNullOrEmpty(ulica))
                    {
                        addrParts.Add(ulica + (string.IsNullOrEmpty(nrDomu) ? "" : " " + nrDomu));
                    }
                    if (!string.IsNullOrEmpty(kodPocztowy) || !string.IsNullOrEmpty(miejscowosc))
                    {
                        addrParts.Add($"{kodPocztowy} {miejscowosc}".Trim());
                    }
                    buyerAddress = string.Join(", ", addrParts);
                }
                else
                {
                    var adresLnk = adres.Elements().FirstOrDefault(e => e.Name.LocalName == "AdresLnk");
                    if (adresLnk != null)
                    {
                        buyerAddress = adresLnk.Elements().FirstOrDefault(e => e.Name.LocalName == "AdresTekst")?.Value ?? string.Empty;
                    }
                }
            }
        }

        // Fa - Dane faktury
        var fa = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Fa");
        if (fa == null)
            throw new ArgumentException("Missing 'Fa' element in XML.");

        var invoiceNumber = fa.Elements().FirstOrDefault(e => e.Name.LocalName == "P_2")?.Value ?? string.Empty;
        var dateStr = fa.Elements().FirstOrDefault(e => e.Name.LocalName == "P_1")?.Value ?? string.Empty;
        DateTime.TryParse(dateStr, out var date);

        var totalAmountStr = fa.Elements().FirstOrDefault(e => e.Name.LocalName == "P_15")?.Value ?? "0";
        decimal.TryParse(totalAmountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var totalAmount);

        // Wiersze
        var items = new List<ParsedKsefInvoiceItem>();
        var wiersze = fa.Elements().Where(e => e.Name.LocalName == "FaWiersz");
        foreach (var wiersz in wiersze)
        {
            var name = wiersz.Elements().FirstOrDefault(e => e.Name.LocalName == "P_7")?.Value ?? "Pozycja";
            
            var qtyStr = wiersz.Elements().FirstOrDefault(e => e.Name.LocalName == "P_8A")?.Value ?? "1";
            decimal.TryParse(qtyStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var qtyDec);
            int qty = (int)qtyDec;
            if (qty < 1) qty = 1;

            var priceStr = wiersz.Elements().FirstOrDefault(e => e.Name.LocalName == "P_9B")?.Value ?? "0";
            decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price);

            var totalStr = wiersz.Elements().FirstOrDefault(e => e.Name.LocalName == "P_11")?.Value ?? "0";
            decimal.TryParse(totalStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var totalPrice);

            if (totalPrice == 0 && price > 0)
            {
                totalPrice = qty * price;
            }

            items.Add(new ParsedKsefInvoiceItem(name, qty, price, totalPrice));
        }

        return new ParsedKsefInvoice(
            invoiceNumber,
            date,
            sellerName,
            sellerNip,
            sellerAddress,
            buyerName,
            buyerNip,
            buyerAddress,
            totalAmount,
            items
        );
    }
}
