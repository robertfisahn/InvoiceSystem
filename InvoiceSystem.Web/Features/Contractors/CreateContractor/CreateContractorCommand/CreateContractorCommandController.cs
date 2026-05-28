using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Contractors.CreateContractor.CreateContractorCommand;

[Route("contractors/create")]
[Tags("Contractors")]
public sealed class CreateContractorCommandController(IMediator mediator) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([FromForm] CreateContractorCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Wystąpił błąd podczas zapisu kontrahenta.");
                return View("~/Features/Contractors/CreateContractor/GetCreateContractorQuery/Index.cshtml", command);
            }

            if (!string.IsNullOrWhiteSpace(command.SessionId) && !string.IsNullOrWhiteSpace(result.CreateInvoiceCommandJson))
            {
                TempData["AiParsedCommand"] = result.CreateInvoiceCommandJson;
                TempData["SuccessMessage"] = "Pomyślnie zarejestrowano nowego kontrahenta. Dane faktury zostały zaimportowane do formularza.";
                return RedirectToAction("Index", "CreateInvoice");
            }

            TempData["SuccessMessage"] = $"Kontrahent '{command.Name}' został pomyślnie dodany.";
            return RedirectToAction("Index", "GetContractorList");
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        return View("~/Features/Contractors/CreateContractor/GetCreateContractorQuery/Index.cshtml", command);
    }
}
