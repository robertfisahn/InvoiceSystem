using MediatR;

namespace InvoiceSystem.Web.Features.Auth.Login;

// LoginResult keeps bool pattern intentionally — Identity returns 3 distinct states
// (Success, LockedOut, InvalidCredentials) that cannot be expressed as exceptions alone.
public record LoginCommand(string? Username, string? Password, bool RememberMe = false) : IRequest<LoginResult>;

public record LoginResult(bool Success, string? Error = null);
