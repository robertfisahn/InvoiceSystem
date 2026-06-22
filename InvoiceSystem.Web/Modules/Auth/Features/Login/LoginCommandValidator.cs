using FluentValidation;

namespace InvoiceSystem.Web.Modules.Auth.Features.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Podaj nazwę użytkownika.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Podaj hasło.");
    }
}
