using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.UpdateInvoice;

[Route("invoices/update")]
public class UpdateInvoiceController(IMediator mediator) : Controller
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
        // Re-assign ID to ensure it's correct from the URL
        var command = viewModel.Command with { Id = id };
        
        var result = await mediator.Send(command);

        if (result.Success)
        {
            return RedirectToAction("Index", "GetInvoiceDetails", new { id });
        }

        ModelState.AddModelError("", result.Error ?? "Wystąpił błąd podczas aktualizacji.");
        
        // Reload data for the view
        var fullViewModel = await mediator.Send(new GetInvoiceForUpdateQuery(id));
        if (fullViewModel == null) return NotFound();

        return View(fullViewModel with { Command = command });
    }
}
