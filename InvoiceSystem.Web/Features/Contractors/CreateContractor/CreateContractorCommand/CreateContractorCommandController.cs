using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Web.Features.Contractors.CreateContractor.GetCreateContractorQuery;

namespace InvoiceSystem.Web.Features.Contractors.CreateContractor.CreateContractorCommand;

[Route("contractors/create")]
[ApiExplorerSettings(IgnoreApi = true)]
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
                
                var warningMsg = !string.IsNullOrWhiteSpace(command.SessionId)
                    ? $"Brak kontrahenta z NIP: {command.TaxId} w bazie danych. Dane zostały wyodrębnione przez AI. Zweryfikuj i zapisz kontrahenta."
                    : null;
                var errorVm = CreateContractorViewModel.From(command, warningMsg);
                return View("~/Features/Contractors/CreateContractor/GetCreateContractorQuery/Index.cshtml", errorVm);
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

        var warningMessage = !string.IsNullOrWhiteSpace(command.SessionId)
            ? $"Brak kontrahenta z NIP: {command.TaxId} w bazie danych. Dane zostały wyodrębnione przez AI. Zweryfikuj i zapisz kontrahenta."
            : null;
        var viewModel = CreateContractorViewModel.From(command, warningMessage);
        return View("~/Features/Contractors/CreateContractor/GetCreateContractorQuery/Index.cshtml", viewModel);
    }
}
