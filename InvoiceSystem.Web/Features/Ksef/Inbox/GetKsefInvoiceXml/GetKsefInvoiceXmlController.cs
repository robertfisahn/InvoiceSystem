using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceXml;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Ksef.Inbox.GetKsefInvoiceXml;

[Route("ksef/inbox")]
public sealed class GetKsefInvoiceXmlController(IMediator mediator) : Controller
{
    [HttpGet("xml/{id:int}")]
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
