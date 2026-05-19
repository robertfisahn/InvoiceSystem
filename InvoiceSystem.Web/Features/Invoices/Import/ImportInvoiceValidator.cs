using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace InvoiceSystem.Web.Features.Invoices.Import;

public sealed class ImportInvoiceValidator : AbstractValidator<ImportInvoiceCommand>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    public ImportInvoiceValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Nie wybrano pliku.")
            .Must(file => file?.Length > 0).WithMessage("Nie wybrano pliku.")
            .Must(file => file?.Length <= MaxFileSize).WithMessage("Plik jest zbyt duży (maksymalnie 10MB).")
            .Must(BeAValidExtension).WithMessage("Nieobsługiwany format pliku. Dozwolone: PDF, JPG, PNG.");
    }

    private bool BeAValidExtension(IFormFile? file)
    {
        if (file == null) return false;
        var extension = Path.GetExtension(file.FileName).ToLower();
        return _allowedExtensions.Contains(extension);
    }
}
