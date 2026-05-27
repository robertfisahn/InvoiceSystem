using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.TestKsefConnection;

public sealed record TestKsefConnectionCommand(string Nip, string ApiKey) : IRequest<TestKsefConnectionResult>;

public sealed record TestKsefConnectionResult(bool Success, string Message);
