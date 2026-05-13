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
        var result = await mediator.Send(new ImportInvoiceCommand(file));
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            
            if (!string.IsNullOrEmpty(result.ExtractedData))
            {
                TempData["ExtractedInvoiceData"] = result.ExtractedData;
                TempData["UploadedFilePath"] = result.FilePath;
                return RedirectToAction("Index", "CreateInvoice");
            }

            return RedirectToAction("Index");
        }

        ViewBag.ErrorMessage = result.Message;
        return View("Import");
    }
}
