using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Invoices.Features.SendToKsef.DownloadInvoiceKsefUpo;

[Route("invoices")]
[Tags("Invoices")]
public sealed class DownloadInvoiceKsefUpoController(IMediator mediator) : Controller
{
    [HttpGet("{id:int}/ksef-upo")]
    public async Task<IActionResult> Index(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DownloadInvoiceKsefUpoQuery(id), cancellationToken);
        if (!result.Success)
            return NotFound(result.ErrorMessage);

        var bytes = System.Text.Encoding.UTF8.GetBytes(result.UpoXml!);
        return File(bytes, "application/xml", $"UPO-{result.KsefNumber}.xml");
    }
}
