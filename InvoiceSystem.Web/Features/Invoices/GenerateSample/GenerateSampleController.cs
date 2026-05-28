using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.GenerateSample;

[Route("invoices/generator")]
[Microsoft.AspNetCore.Http.Tags("Invoices")]
public sealed class GenerateSampleController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new GenerateSampleViewModel());
    }

    /// <summary>
    /// AJAX endpoint — zwraca losowe dane faktury jako JSON.
    /// </summary>
    [HttpGet("randomize")]
    public IActionResult Randomize()
    {
        var model = GenerateSampleHandler.GenerateFakeInvoiceData();
        return Json(model);
    }

    /// <summary>
    /// POST z JSON + format — zwraca wygenerowany plik do pobrania.
    /// </summary>
    [HttpPost("download")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Download([FromForm] string jsonData, [FromForm] string format, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            return BadRequest("Brak danych JSON do wygenerowania pliku.");
        }

        if (!Enum.TryParse<SampleInvoiceFormat>(format, true, out var parsedFormat))
        {
            return BadRequest("Nieprawidłowy format. Dozwolone: pdf, jpg, png.");
        }

        var result = await mediator.Send(new GenerateSampleCommand(jsonData, parsedFormat), cancellationToken);

        return File(result.FileBytes, result.ContentType, result.FileName);
    }
}
