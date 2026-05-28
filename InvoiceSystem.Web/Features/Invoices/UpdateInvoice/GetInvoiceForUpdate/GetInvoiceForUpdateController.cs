using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice.GetInvoiceForUpdate;

[Route("invoices/update")]
[Tags("Invoices")]
public sealed class GetInvoiceForUpdateController(IMediator mediator) : Controller
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Index(int id)
    {
        var viewModel = await mediator.Send(new GetInvoiceForUpdateQuery(id));
        if (viewModel == null) return NotFound();

        return View(viewModel); // Resolves automatically to Index.cshtml in this directory!
    }
}
