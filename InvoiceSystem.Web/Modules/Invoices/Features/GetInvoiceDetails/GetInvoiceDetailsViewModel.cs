using System;
using System.Collections.Generic;
using InvoiceSystem.Web.Modules.Invoices.Domain;
using InvoiceSystem.Web.Modules.Contractors.Domain;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceDetails;

public record GetInvoiceDetailsViewModel(
    int Id,
    string InvoiceNumber,
    DateTime Date,
    ContractorDetailsDto Contractor,
    List<InvoiceItemDto> Items,
    decimal TotalAmount,
    InvoiceStatus Status,
    string? KsefNumber,
    string? KsefTransactionId,
    DateTime? KsefSentAt,
    string? UpoXml
);

public record ContractorDetailsDto(string Name, string? TaxId, string? Address);
public record InvoiceItemDto(string Name, int Quantity, decimal UnitPrice, decimal TotalPrice);
