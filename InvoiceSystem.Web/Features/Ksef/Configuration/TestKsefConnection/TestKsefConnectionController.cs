using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.TestKsefConnection;

[Route("ksef")]
[Microsoft.AspNetCore.Http.Tags("KSeF")]
public sealed class TestKsefConnectionController(IMediator mediator) : Controller
{
    [HttpPost("test-connection")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(TestKsefConnectionResult), Microsoft.AspNetCore.Http.StatusCodes.Status200OK)]
    public async Task<IActionResult> TestConnection(string nip, string apiKey, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new TestKsefConnectionCommand(nip, apiKey), cancellationToken);
        return Ok(result);
    }
}
