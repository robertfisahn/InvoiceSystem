using InvoiceSystem.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Infrastructure.Database;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedInvoicesAsync(context);
        await SeedKsefSettingsAsync(context);
        await SeedKsefIncomingInvoicesAsync(context);
    }

    public static async Task SeedUsersAsync(UserManager<AppUser> userManager)
    {
        if (await userManager.FindByNameAsync("admin") == null)
        {
            var admin = new AppUser
            {
                UserName = "admin",
                Email = "admin@system.pl",
                FullName = "Administrator Systemu",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (!result.Succeeded)
            {
                throw new Exception($"Błąd podczas tworzenia admina: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    private static async Task SeedInvoicesAsync(AppDbContext context)
    {
        if (await context.Invoices.AnyAsync()) return;

        var contractors = new List<Contractor>
        {
            new() { Name = "DevOps Master Sp. z o.o.", TaxId = "777-111-22-33", Address = "ul. Krótka 2, 60-001 Poznań" },
            new() { Name = "CloudSoft Polska", TaxId = "123-456-78-90", Address = "al. Jerozolimskie 100, 00-001 Warszawa" },
            new() { Name = "DataBridge S.A.", TaxId = "987-654-32-10", Address = "ul. Długa 15, 80-001 Gdańsk" },
            new() { Name = "Nexus Technologies", TaxId = "555-222-11-44", Address = "ul. Kwiatowa 7, 30-001 Kraków" },
            new() { Name = "ByteForge Ltd.", TaxId = "333-777-88-99", Address = "ul. Lipowa 3, 50-001 Wrocław" },
            new() { Name = "InfoStream S.A.", TaxId = "111-999-55-66", Address = "ul. Cicha 9, 90-001 Łódź" },
        };

        var invoices = new List<Invoice>
        {
            CreateInvoice("INV/2026/001", contractors[0], new DateTime(2026, 1, 5), "Audyt infrastruktury CI/CD", 1, 8500m),
            CreateInvoice("INV/2026/002", contractors[1], new DateTime(2026, 1, 12), "Migracja do AWS", 1, 15000m),
            CreateInvoice("INV/2026/003", contractors[2], new DateTime(2026, 1, 18), "Integracja API REST", 3, 2200m),
            CreateInvoice("INV/2026/004", contractors[3], new DateTime(2026, 1, 25), "Konsultacje architektoniczne", 4, 1800m),
            CreateInvoice("INV/2026/005", contractors[4], new DateTime(2026, 2, 3), "Optymalizacja bazy danych", 1, 5500m),
            CreateInvoice("INV/2026/006", contractors[5], new DateTime(2026, 2, 8), "Wdrożenie systemu monitoringu", 2, 3200m),
            CreateInvoice("INV/2026/007", contractors[0], new DateTime(2026, 2, 14), "Pipeline Kubernetes", 1, 9800m),
            CreateInvoice("INV/2026/008", contractors[1], new DateTime(2026, 2, 22), "Szkolenie Docker Advanced", 5, 800m),
            CreateInvoice("INV/2026/009", contractors[2], new DateTime(2026, 3, 1), "Opracowanie dokumentacji technicznej", 1, 4400m),
            CreateInvoice("INV/2026/010", contractors[3], new DateTime(2026, 3, 7), "Analiza bezpieczeństwa aplikacji", 1, 11200m),
            CreateInvoice("INV/2026/011", contractors[4], new DateTime(2026, 3, 15), "Refactoring systemu płatności", 2, 7600m),
            CreateInvoice("INV/2026/012", contractors[5], new DateTime(2026, 3, 21), "Implementacja SSO / OAuth2.0", 1, 6300m),
            CreateInvoice("INV/2026/013", contractors[0], new DateTime(2026, 3, 28), "Optymalizacja CI/CD", 1, 2500m),
            CreateInvoice("INV/2026/014", contractors[1], new DateTime(2026, 4, 4), "Konfiguracja Terraform", 1, 13700m),
            CreateInvoice("INV/2026/015", contractors[2], new DateTime(2026, 4, 9), "Projekt architektury mikroserwisów", 1, 19500m),
            CreateInvoice("INV/2026/016", contractors[3], new DateTime(2026, 4, 11), "Testy penetracyjne", 1, 8900m),
            CreateInvoice("INV/2026/017", contractors[4], new DateTime(2026, 4, 14), "Wsparcie techniczne L2", 10, 350m),
            CreateInvoice("INV/2026/018", contractors[5], new DateTime(2026, 4, 15), "Hosting serwerów dedicowanych", 1, 2800m),
            CreateInvoice("INV/2026/019", contractors[0], new DateTime(2026, 4, 16), "Zarządzanie infrastruktukurą cloud", 1, 4200m),
            CreateInvoice("INV/2026/020", contractors[1], new DateTime(2026, 4, 17), "Wdrożenie Observability stack", 1, 7100m),
            CreateInvoice("INV/2026/021", contractors[2], new DateTime(2026, 4, 18), "Budowa API Gateway", 1, 5800m),
            CreateInvoice("INV/2026/022", contractors[3], new DateTime(2026, 4, 19), "Projekt ERD bazy danych", 1, 3400m),
            CreateInvoice("INV/2026/023", contractors[4], new DateTime(2026, 4, 20), "Automatyzacja testów E2E", 1, 6600m),
            CreateInvoice("INV/2026/024", contractors[5], new DateTime(2026, 4, 21), "Backup i disaster recovery", 1, 4700m),
            CreateInvoice("INV/2026/025", contractors[0], new DateTime(2026, 4, 22), "Implementacja cache Redis", 1, 3100m),
            CreateInvoice("INV/2026/026", contractors[1], new DateTime(2026, 4, 23), "Optymalizacja zapytań SQL", 1, 2900m),
            CreateInvoice("INV/2026/027", contractors[2], new DateTime(2026, 4, 23), "Code review i analiza jakości", 1, 1800m),
            CreateInvoice("INV/2026/028", contractors[3], new DateTime(2026, 4, 24), "Wdrożenie CI na GitLab", 1, 3900m),
            CreateInvoice("INV/2026/029", contractors[4], new DateTime(2026, 4, 25), "Konsultacje .NET 9 migration", 2, 4500m),
            CreateInvoice("INV/2026/030", contractors[5], new DateTime(2026, 4, 26), "Projektowanie UX Enterprise systemu", 1, 8200m),
        };

        context.Invoices.AddRange(invoices);
        await context.SaveChangesAsync();
    }

    private static Invoice CreateInvoice(
        string number, Contractor contractor, DateTime date, string itemName, int qty, decimal unitPrice)
    {
        return new Invoice
        {
            InvoiceNumber = number,
            Date = date,
            Contractor = contractor,
            Items = new List<InvoiceItem>
            {
                new()
                {
                    Name = itemName,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    TotalPrice = qty * unitPrice
                }
            }
        };
    }

    private static async Task SeedKsefSettingsAsync(AppDbContext context)
    {
        if (await context.KsefSettings.AnyAsync()) return;

        var placeholderSetting = new KsefSetting
        {
            Nip = "",
            ApiKey = "",
            IsEnabled = false,
            LastSyncDate = null
        };

        context.KsefSettings.Add(placeholderSetting);
        await context.SaveChangesAsync();
    }

    private static async Task SeedKsefIncomingInvoicesAsync(AppDbContext context)
    {
        if (await context.KsefIncomingInvoices.AnyAsync()) return;

        var mockInvoices = new List<KsefIncomingInvoice>
        {
            new()
            {
                KsefNumber = $"5270103391-{DateTime.UtcNow:yyyyMMdd}-FA-A1B2C3D4E5",
                SellerName = "Microsoft Sp. z o.o.",
                SellerNip = "5270103391",
                IssueDate = DateTime.UtcNow.AddDays(-5),
                TotalAmount = 1230.00m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2023/06/29/12648/"">
    <Naglowek>
        <KodFormularza kodSystemowy=""FA (2)"" wersjaSchemy=""1-0E"">FA</KodFormularza>
        <WariantFormularza>2</WariantFormularza>
        <DataWytworzeniaFa>{DateTime.UtcNow.AddDays(-5):yyyy-MM-ddTHH:mm:ssZ}</DataWytworzeniaFa>
    </Naglowek>
    <Podmiot1>
        <DanePodmiotu>
            <NIP>5270103391</NIP>
            <Nazwa>Microsoft Sp. z o.o.</Nazwa>
        </DanePodmiotu>
        <Adres>
            <AdresPol>
                <KodPocztowy>00-121</KodPocztowy>
                <Miejscowosc>Warszawa</Miejscowosc>
                <Ulica>Aleje Jerozolimskie 195A</Ulica>
            </AdresPol>
        </Adres>
    </Podmiot1>
    <Podmiot2>
        <DanePodmiotu>
            <NIP>1111111111</NIP>
            <Nazwa>InvoiceSystem Enterprise</Nazwa>
        </DanePodmiotu>
    </Podmiot2>
    <Fa>
        <KodWaluty>PLN</KodWaluty>
        <P_1>{DateTime.UtcNow.AddDays(-5):yyyy-MM-dd}</P_1>
        <P_2>FV/2026/05/100</P_2>
        <P_13_1>1000.00</P_13_1>
        <P_14_1>230.00</P_14_1>
        <P_15>1230.00</P_15>
        <FaWiersz>
            <NrWierszaFa>1</NrWierszaFa>
            <P_7>Subskrypcja Microsoft 365 Enterprise</P_7>
            <P_8A>1</P_8A>
            <P_9B>1000.00</P_9B>
            <P_11>1000.00</P_11>
            <P_12>23</P_12>
        </FaWiersz>
    </Fa>
</Faktura>"
            },
            new()
            {
                KsefNumber = $"5252344078-{DateTime.UtcNow:yyyyMMdd}-FA-X1Y2Z3W4V5",
                SellerName = "Google Poland Sp. z o.o.",
                SellerNip = "5252344078",
                IssueDate = DateTime.UtcNow.AddDays(-2),
                TotalAmount = 4500.00m,
                ImportStatus = KsefImportStatus.Pending,
                RawXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2023/06/29/12648/"">
    <Naglowek>
        <KodFormularza kodSystemowy=""FA (2)"" wersjaSchemy=""1-0E"">FA</KodFormularza>
        <WariantFormularza>2</WariantFormularza>
        <DataWytworzeniaFa>{DateTime.UtcNow.AddDays(-2):yyyy-MM-ddTHH:mm:ssZ}</DataWytworzeniaFa>
    </Naglowek>
    <Podmiot1>
        <DanePodmiotu>
            <NIP>5252344078</NIP>
            <Nazwa>Google Poland Sp. z o.o.</Nazwa>
        </DanePodmiotu>
        <Adres>
            <AdresPol>
                <KodPocztowy>00-113</KodPocztowy>
                <Miejscowosc>Warszawa</Miejscowosc>
                <Ulica>ul. Emilii Plater 53</Ulica>
            </AdresPol>
        </Adres>
    </Podmiot1>
    <Podmiot2>
        <DanePodmiotu>
            <NIP>1111111111</NIP>
            <Nazwa>InvoiceSystem Enterprise</Nazwa>
        </DanePodmiotu>
    </Podmiot2>
    <Fa>
        <KodWaluty>PLN</KodWaluty>
        <P_1>{DateTime.UtcNow.AddDays(-2):yyyy-MM-dd}</P_1>
        <P_2>G-998/05/2026</P_2>
        <P_13_1>3658.54</P_13_1>
        <P_14_1>841.46</P_14_1>
        <P_15>4500.00</P_15>
        <FaWiersz>
            <NrWierszaFa>1</NrWierszaFa>
            <P_7>Usługi Google Cloud Platform</P_7>
            <P_8A>1</P_8A>
            <P_9B>3658.54</P_9B>
            <P_11>3658.54</P_11>
            <P_12>23</P_12>
        </FaWiersz>
    </Fa>
</Faktura>"
            }
        };

        context.KsefIncomingInvoices.AddRange(mockInvoices);
        await context.SaveChangesAsync();
    }
}
