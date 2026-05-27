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
                    cmd.CommandText = "SELECT Id, SellerName, KsefNumber, length(RawXml) as len, RawXml FROM KsefIncomingInvoices WHERE Id IN (307, 280)";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine($"ID: {reader["Id"]} | Seller: {reader["SellerName"]} | Ksef: {reader["KsefNumber"]} | Len: {reader["len"]}");
                            var xml = reader["RawXml"]?.ToString() ?? "";
                            Console.WriteLine($"Content (first 100 chars): {(xml.Length > 100 ? xml.Substring(0, 100) : xml)}");
                            Console.WriteLine("---------------------------------------------");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
