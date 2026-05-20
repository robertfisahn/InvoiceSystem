using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Invoices.Import;

[Route("invoices/import")]
public sealed class ImportInvoiceController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View("Import", new ImportInvoiceViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile file)
    {
        try
        {
            var result = await mediator.Send(new ImportInvoiceCommand(file));

            var viewModel = new ImportInvoiceViewModel
            {
                SuccessMessage = result.Success ? result.Message : null,
                ErrorMessage = result.Success ? null : result.Message,
                ExtractedText = result.ExtractedText,
                DocumentType = result.DocumentType,
                FilePath = result.FilePath
            };

            return View("Import", viewModel);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var viewModel = new ImportInvoiceViewModel
            {
                ErrorMessage = string.Join("<br/>", ex.Errors.Select(e => e.ErrorMessage))
            };

            return View("Import", viewModel);
        }
    }

    [HttpGet("proceed")]
    public IActionResult Proceed(string filePath)
    {
        TempData["UploadedFileName"] = filePath;
        return RedirectToAction("Index", "CreateInvoice");
    }
}
