using System.Reflection;
using InvoiceSystem.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, params Assembly[] additionalAssemblies)
    {
        var assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
        assemblies.AddRange(additionalAssemblies);

        services.AddValidatorsFromAssemblies(assemblies);
        
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssemblies(assemblies.ToArray());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
