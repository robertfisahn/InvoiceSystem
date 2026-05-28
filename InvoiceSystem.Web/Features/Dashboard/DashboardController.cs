using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Dashboard;

[Route("dashboard")]
[Microsoft.AspNetCore.Http.Tags("Dashboard")]
public sealed class DashboardController(IMediator mediator) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var model = await mediator.Send(new GetDashboardQuery());
        return View(model);
    }
}
