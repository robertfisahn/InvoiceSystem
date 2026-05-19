using FluentValidation;

namespace InvoiceSystem.Web.Features.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Podaj nazwę użytkownika.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Podaj hasło.");
    }
}
