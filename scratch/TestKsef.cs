using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        var url = "https://api-test.ksef.mf.gov.pl/v2/online/Session/AuthorisationChallenge";
        var content = new StringContent("{\"contextIdentifier\":{\"type\":\"onip\",\"identifier\":\"1111111111\"}}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        Console.WriteLine((int)response.StatusCode);
    }
}
