using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        // The V1 and V2 API might have different public key endpoints
        try {
            var response = await client.GetStringAsync("https://ksef-test.mf.gov.pl/api/online/Ksef/PublicKey");
            File.WriteAllText("ksef_pub.txt", response);
            Console.WriteLine("Key written.");
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
        }
    }
}
