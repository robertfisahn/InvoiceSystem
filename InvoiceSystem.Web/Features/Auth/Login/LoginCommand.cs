using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Web.Features.Auth.Login;

public record LoginCommand(string? Username, string? Password, bool RememberMe = false) : IRequest<LoginResult>;

public record LoginResult(bool Success, string? Error = null);

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Podaj nazwę użytkownika.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Podaj hasło.");
    }
}

public class LoginHandler(SignInManager<AppUser> signInManager) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Username ?? string.Empty, 
            request.Password ?? string.Empty, 
            request.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
            return new LoginResult(true);

        if (result.IsLockedOut)
            return new LoginResult(false, "Konto jest zablokowane. Spróbuj ponownie później.");

        return new LoginResult(false, "Nieprawidłowa nazwa użytkownika lub hasło.");
    }
}
