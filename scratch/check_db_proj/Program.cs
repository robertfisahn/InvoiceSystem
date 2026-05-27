using System;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        try
        {
            var dbPath = @"c:\Users\rober\.gemini\antigravity\scratch\InvoiceSystem\InvoiceSystem.Web\InvoiceSystem.db";
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE KsefIncomingInvoices SET RawXml = ''";
                    int count = cmd.ExecuteNonQuery();
                    Console.WriteLine($"Successfully cleared RawXml cache for {count} invoices.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
