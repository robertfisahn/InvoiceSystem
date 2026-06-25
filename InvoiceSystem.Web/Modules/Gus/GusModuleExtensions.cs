using InvoiceSystem.Web.Modules.Gus.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SoapCore;

namespace InvoiceSystem.Web.Modules.Gus
{
    public static class GusModuleExtensions
    {
        public static IServiceCollection AddGusModule(this IServiceCollection services)
        {
            services.AddScoped<IUslugaBIRzewnPubl, UslugaBIRzewnPublMock>();

            return services;
        }

        public static IApplicationBuilder UseGusModule(this IApplicationBuilder app)
        {
            app.UseSoapEndpoint<IUslugaBIRzewnPubl>("/services/regon.asmx", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);
            return app;
        }
    }
}
