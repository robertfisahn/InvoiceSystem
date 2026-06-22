using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Contractors.Features.CreateContractor.GetCreateContractorQuery;

[Route("contractors/create")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class GetCreateContractorQueryController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var viewModel = await mediator.Send(new GetCreateContractorQuery(sessionId), cancellationToken);
            return View(viewModel);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Index", "ImportInvoice");
        }
    }
}
