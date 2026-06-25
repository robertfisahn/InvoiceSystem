using System;
using System.Threading.Tasks;
using InvoiceSystem.Web.Modules.Auth.Domain;
using InvoiceSystem.Web.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceSystem.Web.Modules.Auth
{
    public static class AuthModuleExtensions
    {
        public static IServiceCollection AddAuthModule(this IServiceCollection services)
        {
            services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/auth/login";
                options.LogoutPath = "/auth/logout";
                options.AccessDeniedPath = "/auth/login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Name = "invoice-auth";

                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") || 
                        context.Request.Path.StartsWithSegments("/ksef") ||
                        context.Request.Path.StartsWithSegments("/services") ||
                        context.Request.Path.StartsWithSegments("/invoices/import") ||
                        context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                        (context.Request.Headers["Accept"].ToString().Contains("text/html") && context.Request.Headers["Referer"].ToString().Contains("/swagger")))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api") || 
                        context.Request.Path.StartsWithSegments("/ksef") ||
                        context.Request.Path.StartsWithSegments("/services") ||
                        context.Request.Path.StartsWithSegments("/invoices/import") ||
                        context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Headers["Accept"].ToString().Contains("application/json"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            return services;
        }
    }
}
