using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoicePreview;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoicePreview;

[Route("ksef/inbox")]
public sealed class GetKsefInvoicePreviewController(IMediator mediator) : Controller
{
    [HttpGet("preview/{id:int}")]
    public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetKsefInvoicePreviewQuery(id), cancellationToken);
        if (!result.Success)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        return Json(new
        {
            success = true,
            invoiceNumber = result.InvoiceNumber,
            date = result.Date,
            sellerName = result.SellerName,
            sellerNip = result.SellerNip,
            sellerAddress = result.SellerAddress,
            buyerName = result.BuyerName,
            buyerNip = result.BuyerNip,
            buyerAddress = result.BuyerAddress,
            totalAmount = result.TotalAmount,
            items = result.Items
        });
    }
}
