using MediatR;

namespace InvoiceSystem.Web.Features.Ksef.Configuration.GetKsefConfiguration;

public sealed record GetKsefConfigurationQuery : IRequest<KsefConfigurationViewModel>;
