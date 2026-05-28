using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice.UpdateInvoiceCommand;

[Route("invoices/update")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class UpdateInvoiceCommandController(IMediator mediator) : Controller
{
    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int id, [FromForm] UpdateInvoiceCommand command)
    {
        if (id != command.Id) return BadRequest();

        try
        {
            await mediator.Send(command);
            return RedirectToAction("Index", "GetInvoiceDetails", new { id = command.Id });
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
                ModelState.AddModelError($"Command.{error.PropertyName}", error.ErrorMessage);
        }

        var refreshViewModel = await mediator.Send(new GetInvoiceForUpdate.GetInvoiceForUpdateQuery(command.Id));
        if (refreshViewModel == null) return NotFound();

        return View("~/Features/Invoices/UpdateInvoice/GetInvoiceForUpdate/Index.cshtml", refreshViewModel with { Command = command });
    }
}
