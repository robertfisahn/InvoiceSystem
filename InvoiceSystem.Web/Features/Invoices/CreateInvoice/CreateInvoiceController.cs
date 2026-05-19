using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.CreateInvoice;

[Route("invoices/create")]
public sealed class CreateInvoiceController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = await mediator.Send(new GetCreateInvoiceQuery());

        if (TempData["UploadedFileName"] is string fileName)
            viewModel = viewModel with { Command = new CreateInvoiceCommand { FilePath = fileName } };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([FromForm] CreateInvoiceCommand command)
    {
        try
        {
            var invoiceId = await mediator.Send(command);
            return RedirectToAction("Index", "GetInvoiceDetails", new { id = invoiceId });
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
                ModelState.AddModelError($"Command.{error.PropertyName}", error.ErrorMessage);
        }

        var refreshViewModel = await mediator.Send(new GetCreateInvoiceQuery());
        return View(refreshViewModel with { Command = command });
    }
}
