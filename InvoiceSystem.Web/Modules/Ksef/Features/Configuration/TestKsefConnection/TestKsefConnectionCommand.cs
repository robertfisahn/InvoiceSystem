using MediatR;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Configuration.TestKsefConnection;

public sealed record TestKsefConnectionCommand(string Nip, string ApiKey) : IRequest<TestKsefConnectionResult>;

public sealed record TestKsefConnectionResult(bool Success, string Message);
