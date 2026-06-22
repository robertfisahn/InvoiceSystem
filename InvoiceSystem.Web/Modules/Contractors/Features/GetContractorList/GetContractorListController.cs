using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InvoiceSystem.Web.Modules.Contractors.Features.GetContractorList;

[Route("contractors")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class GetContractorListController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var contractors = await mediator.Send(new GetContractorListQuery());
        return View("~/Modules/Contractors/Features/GetContractorList/Index.cshtml", contractors);
    }
}
