using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Invoices.Features.Import.ImportInvoice;

[Route("invoices/import")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ImportInvoiceController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var viewModel = new ImportInvoiceViewModel
        {
            ErrorMessage = TempData["ErrorMessage"] as string,
            SuccessMessage = TempData["SuccessMessage"] as string,
            ExtractedText = TempData["ExtractedText"] as string,
            DocumentType = TempData["DocumentType"] as string,
            FilePath = TempData["FilePath"] as string
        };

        return View(viewModel); // Resolves automatically to Index.cshtml in this directory!
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

            return View(viewModel);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var viewModel = new ImportInvoiceViewModel
            {
                ErrorMessage = string.Join("<br/>", ex.Errors.Select(e => e.ErrorMessage))
            };

            return View(viewModel);
        }
    }
}
