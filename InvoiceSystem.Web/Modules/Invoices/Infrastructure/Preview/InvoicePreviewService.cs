using System;
using System.Linq;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceDetails;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;

namespace InvoiceSystem.Web.Modules.Invoices.Infrastructure.Preview;

public sealed class InvoicePreviewService : IInvoicePreviewService
{
    public InvoicePreviewDto MapFromInvoice(GetInvoiceDetailsViewModel viewModel)
    {
        var statusText = viewModel.Status switch
        {
            InvoiceStatus.Draft => "Szkic",
            InvoiceStatus.Confirmed => "Zatwierdzona",
            InvoiceStatus.Paid => "Opłacona",
            _ => "Szkic"
        };

        var statusClass = viewModel.Status switch
        {
            InvoiceStatus.Draft => "draft",
            InvoiceStatus.Confirmed => "overdue",
            InvoiceStatus.Paid => "confirmed",
            _ => "draft"
        };

        return new InvoicePreviewDto
        {
            Title = "FAKTURA",
            InvoiceNumber = viewModel.InvoiceNumber,
            Date = viewModel.Date,
            KsefNumber = viewModel.KsefNumber,
            KsefTransactionId = viewModel.KsefTransactionId?.Contains(':') == true
                ? viewModel.KsefTransactionId.Split(':')[1]
                : viewModel.KsefTransactionId,
            StatusText = statusText,
            StatusClass = statusClass,
            SellerName = "InvoiceSystem Enterprise",
            SellerNip = "1234567890",
            SellerAddress = "ul. Technologiczna 12\n80-001 Gdańsk",
            BuyerName = viewModel.Contractor.Name,
            BuyerNip = viewModel.Contractor.TaxId ?? string.Empty,
            BuyerAddress = viewModel.Contractor.Address ?? string.Empty,
            Items = viewModel.Items.Select(i => new InvoicePreviewItemDto
            {
                Name = i.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            TotalAmount = viewModel.TotalAmount,
            FooterText = "Faktura wygenerowana automatycznie w systemie InvoiceSystem. Dziękujemy!"
        };
    }

    public InvoicePreviewDto MapFromKsef(ParsedKsefInvoice parsed, string? ksefNumber)
    {
        return new InvoicePreviewDto
        {
            Title = "FAKTURA KSeF",
            InvoiceNumber = parsed.InvoiceNumber,
            Date = parsed.Date,
            KsefNumber = ksefNumber,
            StatusText = "Pobrana z KSeF",
            StatusClass = "confirmed",
            SellerName = parsed.SellerName,
            SellerNip = parsed.SellerNip,
            SellerAddress = parsed.SellerAddress,
            BuyerName = string.IsNullOrEmpty(parsed.BuyerName) ? "InvoiceSystem Enterprise" : parsed.BuyerName,
            BuyerNip = string.IsNullOrEmpty(parsed.BuyerNip) ? "1111111111" : parsed.BuyerNip,
            BuyerAddress = string.IsNullOrEmpty(parsed.BuyerAddress) ? "ul. Technologiczna 12" : parsed.BuyerAddress,
            Items = parsed.Items.Select(i => new InvoicePreviewItemDto
            {
                Name = i.Name,
                Quantity = (double)i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            TotalAmount = parsed.TotalAmount,
            FooterText = "Faktura pobrana automatycznie z Krajowego Systemu e-Faktur (KSeF). Dziękujemy!"
        };
    }
}
