using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceList;

[Route("invoices")]
[Microsoft.AspNetCore.Http.Tags("Invoices")]
public sealed class GetInvoiceListController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = await mediator.Send(new GetInvoiceListQuery());
        return View(viewModel);
    }
}
