using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceDetails;

public class GetInvoiceDetailsController(IMediator mediator) : Controller
{
    [HttpGet("invoices/{id:int}")]
    public async Task<IActionResult> Index(int id)
    {
        var result = await mediator.Send(new GetInvoiceDetailsQuery(id));
        if (result == null) 
            return NotFound();
        
        return View(result);
    }
}
