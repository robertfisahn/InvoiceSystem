using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice.GetCreateInvoiceQuery;

[Route("invoices/create")]
[Tags("Invoices")]
public sealed class GetCreateInvoiceQueryController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = await mediator.Send(new GetCreateInvoiceQuery());

        if (TempData["AiParsedCommand"] is string jsonCommand)
        {
            try
            {
                var command = System.Text.Json.JsonSerializer.Deserialize<CreateInvoiceCommand.CreateInvoiceCommand>(jsonCommand);
                if (command is not null)
                {
                    viewModel = viewModel with { Command = command };
                }
            }
            catch
            {
                // Ignorujemy błędy deserializacji, fallback do standardowego zachowania
            }
        }
        else if (TempData["UploadedFileName"] is string fileName)
        {
            viewModel = viewModel with { Command = new CreateInvoiceCommand.CreateInvoiceCommand { FilePath = fileName } };
        }

        return View(viewModel); // Resolves automatically to Index.cshtml in this directory!
    }
}
