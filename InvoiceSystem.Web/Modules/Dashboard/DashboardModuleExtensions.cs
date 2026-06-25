using Microsoft.Extensions.DependencyInjection;

namespace InvoiceSystem.Web.Modules.Dashboard
{
    public static class DashboardModuleExtensions
    {
        public static IServiceCollection AddDashboardModule(this IServiceCollection services)
        {
            // Register module-specific services here if needed in the future
            return services;
        }
    }
}
