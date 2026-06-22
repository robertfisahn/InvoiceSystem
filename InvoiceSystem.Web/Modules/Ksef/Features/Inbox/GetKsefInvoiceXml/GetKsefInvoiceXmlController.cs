using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoiceXml;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Inbox.GetKsefInvoiceXml;

[Route("ksef/inbox")]
[Microsoft.AspNetCore.Http.Tags("KSeF")]
public sealed class GetKsefInvoiceXmlController(IMediator mediator) : Controller
{
    [HttpGet("xml/{id:int}")]
    [Produces("application/xml")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetXml(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetKsefInvoiceXmlQuery(id), cancellationToken);
        if (!result.Success)
        {
            return BadRequest($"Nie udało się pobrać pliku XML z KSeF: {result.ErrorMessage}");
        }

        return Content(result.RawXml!, "application/xml");
    }
}
