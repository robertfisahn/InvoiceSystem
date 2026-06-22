using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GenerateSample;

// Modele danych pomocnicze do wstrzyknięcia do dokumentu (nieużywane w bazie, stąd brak w encjach Domeny)
public record SampleInvoiceModel(
    string InvoiceNumber, 
    DateTime IssueDate, 
    SampleCompany Seller, 
    SampleCompany Buyer, 
    List<SampleInvoiceItem> Items);

public record SampleCompany(string CompanyName, string Street, string City, string ZipCode, string Nip);

public record SampleInvoiceItem(string Name, decimal Price, int Quantity)
{
    public decimal Total => Price * Quantity;
}

// QuestPDF Document Layout
internal sealed class SampleInvoiceDocument(SampleInvoiceModel Model) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));
                
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"Faktura VAT nr {Model.InvoiceNumber}")
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                
                column.Item().Text(text =>
                {
                    text.Span("Data wystawienia: ").SemiBold();
                    text.Span(Model.IssueDate.ToString("d"));
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Component(new AddressComponent("Sprzedawca", Model.Seller));
                row.RelativeItem().Component(new AddressComponent("Nabywca", Model.Buyer));
            });

            column.Item().PaddingVertical(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            column.Item().Element(ComposeTable);

            var totalPrice = Model.Items.Sum(x => x.Total);
            column.Item().PaddingTop(15).AlignRight().Text($"Razem do zapłaty: {totalPrice:C}")
                .FontSize(14).SemiBold();
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn();
                columns.ConstantColumn(80);
                columns.ConstantColumn(80);
                columns.ConstantColumn(80);
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("#");
                header.Cell().Element(CellStyle).Text("Nazwa produktu/usługi");
                header.Cell().Element(CellStyle).AlignRight().Text("Cena jedn.");
                header.Cell().Element(CellStyle).AlignRight().Text("Ilość");
                header.Cell().Element(CellStyle).AlignRight().Text("Wartość");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                }
            });

            var step = 1;
            foreach (var item in Model.Items)
            {
                table.Cell().Element(CellStyle).Text(step.ToString());
                table.Cell().Element(CellStyle).Text(item.Name);
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Price:C}");
                table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Total:C}");

                step++;

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Strona ");
            x.CurrentPageNumber();
            x.Span(" z ");
            x.TotalPages();
        });
    }
}

// Sub-component for rendering address blocks
internal class AddressComponent(string title, SampleCompany company) : IComponent
{
    public void Compose(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(5).Text(title).SemiBold().FontSize(12).FontColor(Colors.Grey.Darken1);
            column.Item().Text(company.CompanyName).Bold();
            column.Item().Text(company.Street);
            column.Item().Text($"{company.ZipCode} {company.City}");
            column.Item().Text($"NIP: {company.Nip}");
        });
    }
}
