using Microsoft.Extensions.DependencyInjection;

namespace InvoiceSystem.Web.Modules.Contractors
{
    public static class ContractorsModuleExtensions
    {
        public static IServiceCollection AddContractorsModule(this IServiceCollection services)
        {
            // Register module-specific services here if needed in the future
            return services;
        }
    }
}
