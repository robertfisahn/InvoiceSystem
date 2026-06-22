using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Invoices.Features.SendToKsef.DownloadInvoiceKsefXml;

[Route("invoices")]
[Tags("Invoices")]
public sealed class DownloadInvoiceKsefXmlController(IMediator mediator) : Controller
{
    [HttpGet("{id:int}/ksef-xml")]
    public async Task<IActionResult> Index(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DownloadInvoiceKsefXmlQuery(id), cancellationToken);
        if (!result.Success)
            return NotFound(result.ErrorMessage);

        var bytes = System.Text.Encoding.UTF8.GetBytes(result.Xml!);
        return File(bytes, "application/xml", $"KSEF-{result.InvoiceNumber}.xml");
    }
}
