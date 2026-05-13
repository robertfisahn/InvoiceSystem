using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using InvoiceSystem.Web.Features.Invoices.Import;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

public class CreateInvoiceController(IMediator mediator) : Controller
{
    [HttpGet("invoices/create")]
    public async Task<IActionResult> Index()
    {
        var viewModel = await mediator.Send(new GetCreateInvoiceQuery());
        
        if (TempData["ExtractedInvoiceData"] is string jsonData)
        {
            try 
            {
                var extracted = JsonSerializer.Deserialize<ExtractedInvoiceData>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (extracted != null)
                {
                    var command = new CreateInvoiceCommand
                    {
                        InvoiceNumber = extracted.InvoiceNumber,
                        Date = DateTime.TryParse(extracted.Date, out var d) ? d : DateTime.Now,
                        FilePath = TempData["UploadedFilePath"]?.ToString(),
                        Items = extracted.Items.Select(i => new CreateInvoiceItemCommand 
                        { 
                            Name = i.Name, 
                            Quantity = i.Quantity, 
                            Price = i.Price 
                        }).ToList()
                    };
                    
                    viewModel = viewModel with { Command = command };
                    ViewBag.InfoMessage = "Dane zostały automatycznie wyodrębnione przez AI. Proszę o weryfikację.";
                }
            }
            catch { /* Ignorujemy błędy parsowania, najwyżej formularz będzie pusty */ }
        }

        return View(viewModel);
    }

    [HttpPost("invoices/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([FromForm] CreateInvoiceCommand command)
    {
        try
        {
            var result = await mediator.Send(command);
            if (result.Success)
                return RedirectToAction("Index", "GetInvoiceList");

            ModelState.AddModelError(string.Empty, result.Error ?? "Wystąpił błąd podczas zapisu.");
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                // Musimy zmapować PropertyName na Command.PropertyName ponieważ model w widoku to CreateInvoiceViewModel
                ModelState.AddModelError($"Command.{error.PropertyName}", error.ErrorMessage);
            }
        }

        // W razie błędu musimy odświeżyć listę kontrahentów
        var refreshViewModel = await mediator.Send(new GetCreateInvoiceQuery());
        return View(refreshViewModel with { Command = command });
    }
}
