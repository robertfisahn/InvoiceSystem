using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

[Route("invoices/update")]
public sealed class UpdateInvoiceController(IMediator mediator) : Controller
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Index(int id)
    {
        var viewModel = await mediator.Send(new GetInvoiceForUpdateQuery(id));
        if (viewModel == null) return NotFound();

        return View(viewModel);
    }

    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int id, UpdateInvoiceViewModel viewModel)
    {
        var command = viewModel.Command with { Id = id };

        try
        {
            await mediator.Send(command);
            return RedirectToAction("Index", "GetInvoiceDetails", new { id });
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
                ModelState.AddModelError($"Command.{error.PropertyName}", error.ErrorMessage);
        }

        var fullViewModel = await mediator.Send(new GetInvoiceForUpdateQuery(id));
        if (fullViewModel == null) return NotFound();
        return View(fullViewModel with { Command = command });
    }
}
