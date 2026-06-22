using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Invoices.Features.GetInvoiceList;

[Route("invoices")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class GetInvoiceListController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = await mediator.Send(new GetInvoiceListQuery());
        return View(viewModel);
    }
}
