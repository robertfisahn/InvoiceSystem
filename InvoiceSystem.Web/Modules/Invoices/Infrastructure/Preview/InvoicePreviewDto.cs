using System;
using System.Collections.Generic;

namespace InvoiceSystem.Web.Modules.Invoices.Infrastructure.Preview;

public sealed class InvoicePreviewDto
{
    public string Title { get; set; } = "FAKTURA";
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? KsefNumber { get; set; }
    public string? KsefTransactionId { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    
    public string SellerName { get; set; } = "InvoiceSystem Enterprise";
    public string SellerNip { get; set; } = "1234567890";
    public string SellerAddress { get; set; } = "ul. Technologiczna 12\n80-001 Gdańsk";

    public string BuyerName { get; set; } = string.Empty;
    public string BuyerNip { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;

    public List<InvoicePreviewItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    
    public string FooterText { get; set; } = "Faktura wygenerowana automatycznie. Dziękujemy!";
}

public sealed class InvoicePreviewItemDto
{
    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
