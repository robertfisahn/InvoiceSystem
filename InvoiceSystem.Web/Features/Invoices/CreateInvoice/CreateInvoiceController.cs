using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

public class CreateInvoiceController(IMediator mediator) : Controller
{
    [HttpGet("invoices/create")]
    public async Task<IActionResult> Index()
    {
        var viewModel = await mediator.Send(new GetCreateInvoiceQuery());
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
