using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoicePreview;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoicePreview;

[Route("ksef/inbox")]
[Microsoft.AspNetCore.Http.Tags("KSeF")]
public sealed class GetKsefInvoicePreviewController(IMediator mediator) : Controller
{
    [HttpGet("preview/{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(GetKsefInvoicePreviewResult), Microsoft.AspNetCore.Http.StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetKsefInvoicePreviewQuery(id), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage ?? "Nie udało się pobrać podglądu faktury.");
        }

        return Ok(result);
    }
}
