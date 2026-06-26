using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Timeout;

namespace InvoiceSystem.Web.Modules.Ksef
{
    public static class KsefModuleExtensions
    {
        public static IServiceCollection AddKsefModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IKsefClient, KsefClient>()
                .AddStandardResilienceHandler(options =>
                {
                    // Customize retry to include HTTP 429 Too Many Requests and common transient errors
                    options.Retry.ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>()
                        .HandleResult(response =>
                            response.StatusCode == HttpStatusCode.InternalServerError || // 500
                            response.StatusCode == HttpStatusCode.BadGateway ||          // 502
                            response.StatusCode == HttpStatusCode.ServiceUnavailable ||  // 503
                            response.StatusCode == HttpStatusCode.GatewayTimeout ||      // 504
                            response.StatusCode == HttpStatusCode.RequestTimeout ||      // 408
                            response.StatusCode == (HttpStatusCode)429);                 // 429 Too Many Requests

                    // Exponential backoff configuration
                    options.Retry.BackoffType = DelayBackoffType.Exponential;
                    options.Retry.UseJitter = true;
                    options.Retry.MaxRetryAttempts = 3;
                    options.Retry.Delay = TimeSpan.FromSeconds(2);
                });

            services.AddSingleton<IKsefSyncLock, KsefSyncLock>();
            services.AddScoped<IKsefSyncService, KsefSyncService>();
            services.AddHostedService<KsefSyncBackgroundService>();

            return services;
        }

        public static IEndpointRouteBuilder MapKsefEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/invoices-db", async (AppDbContext dbContext) => {
                var list = await dbContext.Invoices
                    .Select(i => new { i.Id, i.InvoiceNumber, i.Status, i.KsefNumber, i.KsefTransactionId })
                    .ToListAsync();
                return Results.Json(list);
            }).AllowAnonymous();

            endpoints.MapGet("/dump/{id:int}", async (int id, AppDbContext dbContext) => {
                var invoice = await dbContext.Invoices
                    .Include(i => i.Contractor)
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == id);
                if (invoice == null) return Results.NotFound("Not found");
                var setting = await dbContext.KsefSettings.FirstOrDefaultAsync();
                var xml = KsefXmlSerializer.SerializeToFa3(invoice, setting?.Nip ?? "1234567890");
                return Results.Content(xml, "application/xml");
            }).AllowAnonymous();

            return endpoints;
        }
    }
}
