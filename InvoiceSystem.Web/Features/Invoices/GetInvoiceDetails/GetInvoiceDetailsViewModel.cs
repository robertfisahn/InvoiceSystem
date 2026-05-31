using System;
using System.Collections.Generic;
using InvoiceSystem.Web.Domain.Entities;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceDetails;

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
