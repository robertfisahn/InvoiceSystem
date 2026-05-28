using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceDetails;

[Route("invoices")]
[Microsoft.AspNetCore.Http.Tags("Invoices")]
public sealed class GetInvoiceDetailsController(IMediator mediator) : Controller
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Index(int id)
    {
        var result = await mediator.Send(new GetInvoiceDetailsQuery(id));
        if (result == null) 
            return NotFound();
        
        return View(result);
    }
}
