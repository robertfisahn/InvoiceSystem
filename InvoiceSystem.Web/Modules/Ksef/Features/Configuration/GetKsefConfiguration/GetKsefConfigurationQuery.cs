using MediatR;

using InvoiceSystem.Web.Modules.Ksef.Features.Configuration;

namespace InvoiceSystem.Web.Modules.Ksef.Features.Configuration.GetKsefConfiguration;

public sealed record GetKsefConfigurationQuery : IRequest<KsefConfigurationViewModel>;
