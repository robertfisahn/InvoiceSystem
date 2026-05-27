using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.SaveKsefConfiguration;

public sealed record SaveKsefConfigurationCommand(KsefConfigurationViewModel Model) : IRequest<Unit>;
