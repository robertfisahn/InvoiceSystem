using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InvoiceSystem.Web.Features.Contractors.GetContractorList;

[Route("contractors")]
public sealed class GetContractorListController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var contractors = await mediator.Send(new GetContractorListQuery());
        return View("~/Features/Contractors/GetContractorList/Index.cshtml", contractors);
    }
}
