using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.GetInvoiceList;

public class GetInvoiceListController(IMediator mediator) : Controller
{
    [HttpGet("invoices")]
    public async Task<IActionResult> Index()
    {
        var viewModel = await mediator.Send(new GetInvoiceListQuery());
        return View(viewModel);
    }
}
