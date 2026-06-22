using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Invoices.Features.CreateInvoice.CreateInvoiceCommand;

[Route("invoices/create")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class CreateInvoiceCommandController(IMediator mediator) : Controller
{
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

        var refreshViewModel = await mediator.Send(new GetCreateInvoiceQuery.GetCreateInvoiceQuery());
        return View("~/Modules/Invoices/Features/CreateInvoice/GetCreateInvoiceQuery/Index.cshtml", refreshViewModel with { Command = command });
    }
}
