using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.TestKsefConnection;

[Route("ksef")]
public sealed class TestKsefConnectionController(IMediator mediator) : Controller
{
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection(string nip, string apiKey, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new TestKsefConnectionCommand(nip, apiKey), cancellationToken);
        return Json(new { success = result.Success, message = result.Message });
    }
}
