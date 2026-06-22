using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace InvoiceSystem.Web.Modules.Gus.Infrastructure;

public sealed class UslugaBIRzewnPublMock(IHttpContextAccessor httpContextAccessor) : IUslugaBIRzewnPubl
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Task<string> Zaloguj(string pKluczUzytkownika)
    {
        if (pKluczUzytkownika == "abcde12345abcde12345" || pKluczUzytkownika == "mock_api_key_123")
        {
            return Task.FromResult("mock_session_sid_999");
        }

        throw new FaultException("Invalid user key (pKluczUzytkownika).");
    }

    public Task<string> DaneSzukajJednostki(ParametryWyszukiwania pParametryWyszukiwania)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var sid = httpContext?.Request.Headers["sid"].ToString();

        if (string.IsNullOrEmpty(sid) || sid != "mock_session_sid_999")
        {
            throw new FaultException("Invalid or missing session identifier (sid).");
        }

        var nip = pParametryWyszukiwania?.Nip;
        if (string.IsNullOrWhiteSpace(nip))
        {
            throw new FaultException("Search criteria NIP must be provided.");
        }

        if (nip == "9999999999")
        {
            throw new FaultException("Internal server error or company registry search failure.");
        }

        string regon = "123456789";
        string nazwa = "TEST CONTRACTOR CO";
        string wojewodztwo = "POMORSKIE";
        string miejscowosc = "Gdańsk";
        string kodPocztowy = "80-001";
        string ulica = "ul. Technologiczna 12";
        string statusVat = "ACTIVE";

        if (nip == "5261040825")
        {
            nazwa = "GUS SANDBOX TEST COMPANY";
            regon = "140000001";
            statusVat = "ACTIVE";
        }
        else if (nip == "2222222222")
        {
            nazwa = "Firma Testowa NIP 222 (Aktywny VAT)";
            regon = "222222222";
            statusVat = "ACTIVE";
        }
        else if (nip == "3333333333")
        {
            nazwa = "Firma Testowa NIP 333 (Zwolniony z VAT)";
            regon = "333333333";
            statusVat = "EXEMPT";
        }
        else if (nip == "4444444444")
        {
            nazwa = "Firma Testowa NIP 444 (Niezarejestrowany)";
            regon = "444444444";
            statusVat = "NOT_REGISTERED";
        }
        else if (nip == "5555555555")
        {
            string notFoundXml = """
            <root>
               <dane>
                  <ErrorCode>4</ErrorCode>
                  <ErrorMessagePl>Nie znaleziono podmiotu dla podanych kryteriów wyszukiwania.</ErrorMessagePl>
               </dane>
            </root>
            """;
            return Task.FromResult(notFoundXml);
        }

        string resultXml = $"""
        <root>
           <dane>
              <Regon>{regon}</Regon>
              <Nazwa>{nazwa}</Nazwa>
              <Wojewodztwo>{wojewodztwo}</Wojewodztwo>
              <Miejscowosc>{miejscowosc}</Miejscowosc>
              <KodPocztowy>{kodPocztowy}</KodPocztowy>
              <Ulica>{ulica}</Ulica>
              <StatusVat>{statusVat}</StatusVat>
           </dane>
        </root>
        """;

        return Task.FromResult(resultXml);
    }

    public Task<bool> Wyloguj(string pIdentyfikatorSesji)
    {
        if (pIdentyfikatorSesji == "mock_session_sid_999")
        {
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
