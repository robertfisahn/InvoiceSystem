using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.Import;

[Route("invoices/import")]
public class ImportInvoiceController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View("Import");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile file)
    {
        try
        {
            var result = await mediator.Send(new ImportInvoiceCommand(file));
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                TempData["UploadedFileName"] = result.FilePath;
                return RedirectToAction("Index", "CreateInvoice");
            }

            ViewBag.ErrorMessage = result.Message;
        }
        catch (FluentValidation.ValidationException ex)
        {
            ViewBag.ErrorMessage = string.Join("<br/>", ex.Errors.Select(e => e.ErrorMessage));
        }

        return View("Import");
    }
}
