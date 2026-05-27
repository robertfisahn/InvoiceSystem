using System;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.ImportKsefInvoice;

public sealed class ImportKsefInvoiceCommandHandler(AppDbContext dbContext) 
    : IRequestHandler<ImportKsefInvoiceCommand, ImportKsefInvoiceResult>
{
    public async Task<ImportKsefInvoiceResult> Handle(ImportKsefInvoiceCommand request, CancellationToken cancellationToken)
    {
        var incoming = await dbContext.KsefIncomingInvoices.FindAsync([request.Id], cancellationToken);
        if (incoming == null)
        {
            return new ImportKsefInvoiceResult(false, null, null, "Faktura KSeF nie została znaleziona.");
        }

        if (incoming.ImportStatus != KsefImportStatus.Pending)
        {
            return new ImportKsefInvoiceResult(false, null, null, "Faktura została już zaimportowana lub zignorowana.");
        }

        try
        {
            // Parse XML to extract invoice details
            var parsed = KsefXmlParser.ParseFa2(incoming.RawXml);

            // Start transaction for atomic execution
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            // Find or create Contractor (Seller)
            var contractor = await dbContext.Contractors
                .FirstOrDefaultAsync(c => c.TaxId == parsed.SellerNip, cancellationToken);

            if (contractor == null)
            {
                contractor = new Contractor
                {
                    Name = parsed.SellerName,
                    TaxId = parsed.SellerNip,
                    Address = parsed.SellerAddress
                };
                dbContext.Contractors.Add(contractor);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            // Create system Invoice as a confirmed purchase invoice
            var newInvoice = new Invoice
            {
                InvoiceNumber = parsed.InvoiceNumber,
                Date = parsed.Date,
                ContractorId = contractor.Id,
                Status = InvoiceStatus.Confirmed,
                KsefNumber = incoming.KsefNumber,
                KsefSentAt = incoming.IssueDate
            };

            dbContext.Invoices.Add(newInvoice);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Map and add items
            foreach (var item in parsed.Items)
            {
                var invoiceItem = new InvoiceItem
                {
                    InvoiceId = newInvoice.Id,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                dbContext.InvoiceItems.Add(invoiceItem);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // Update buffer import status
            incoming.ImportStatus = KsefImportStatus.Imported;
            incoming.ImportedInvoiceId = newInvoice.Id;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new ImportKsefInvoiceResult(true, parsed.InvoiceNumber, parsed.SellerName, null);
        }
        catch (Exception ex)
        {
            return new ImportKsefInvoiceResult(false, null, null, ex.Message);
        }
    }
}
