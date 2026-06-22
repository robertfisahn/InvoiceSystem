using InvoiceSystem.Web.Modules.Auth.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace InvoiceSystem.Web.Modules.Auth.Features.Login;

public sealed class LoginHandler(SignInManager<AppUser> signInManager)
    : IRequestHandler<LoginCommand, LoginResult>
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
