using System.IO;
using InvoiceSystem.Web.Infrastructure.Configuration;
using InvoiceSystem.Web.Infrastructure.Services.Hash;
using InvoiceSystem.Web.Infrastructure.Services.Storage;
using InvoiceSystem.Web.Modules.Invoices.Infrastructure.Llm;
using InvoiceSystem.Web.Modules.Invoices.Infrastructure.Ocr;
using InvoiceSystem.Web.Modules.Invoices.Infrastructure.Preview;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceSystem.Web.Modules.Invoices
{
    public static class InvoicesModuleExtensions
    {
        public static IServiceCollection AddInvoicesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<StorageSettings>()
                .Configure<IWebHostEnvironment>((options, env) =>
                {
                    var configPath = configuration.GetSection("Storage").GetValue<string>("RootPath") ?? "App_Data/Storage/Incoming";
                    options.RootPath = Path.Combine(env.ContentRootPath, configPath);
                });

            services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IFileHashService, FileHashService>();
            services.AddScoped<IDocumentOcrService, DocumentOcrService>();
            services.AddScoped<ILlmService, LlmService>();
            services.AddScoped<IInvoicePreviewService, InvoicePreviewService>();

            return services;
        }
    }
}
