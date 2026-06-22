using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;

namespace InvoiceSystem.Web.Modules.Gus.Infrastructure;

[ServiceContract(Name = "IUslugaBIRzewnPubl", Namespace = "http://CIS/BIR/PUBL/2014/07")]
public interface IUslugaBIRzewnPubl
{
    [OperationContract(Action = "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/Zaloguj", ReplyAction = "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/ZalogujResponse")]
    Task<string> Zaloguj(string pKluczUzytkownika);

    [OperationContract(Action = "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/DaneSzukajJednostki", ReplyAction = "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/DaneSzukajJednostkiResponse")]
    Task<string> DaneSzukajJednostki(ParametryWyszukiwania pParametryWyszukiwania);

    [OperationContract(Action = "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/Wyloguj", ReplyAction = "http://CIS/BIR/PUBL/2014/07/IUslugaBIRzewnPubl/WylogujResponse")]
    Task<bool> Wyloguj(string pIdentyfikatorSesji);
}

[DataContract(Namespace = "http://CIS/BIR/PUBL/2014/07/DataContract")]
public class ParametryWyszukiwania
{
    [DataMember(EmitDefaultValue = false)]
    public string? Nip { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public string? Regon { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public string? Krs { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public string? Nipy { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public string? Regony { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public string? Krsy { get; set; }
}
