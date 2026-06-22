using MediatR;

using InvoiceSystem.Web.Modules.Ksef.Features.Configuration;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Configuration.SaveKsefConfiguration;

public sealed record SaveKsefConfigurationCommand(KsefConfigurationViewModel Model) : IRequest<Unit>;
