using InvoiceSystem.Web.Domain.Entities;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Ksef;
using InvoiceSystem.Web.Infrastructure.Configuration;
using InvoiceSystem.Web.Infrastructure.Behaviors;
using InvoiceSystem.Web.Infrastructure.Services.Storage;
using InvoiceSystem.Web.Infrastructure.Services.Hash;
using InvoiceSystem.Web.Infrastructure.Services.Ocr;
using InvoiceSystem.Web.Infrastructure.Services.Llm;
using InvoiceSystem.Web.Infrastructure.Services.Preview;
using InvoiceSystem.Web.Setup;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// MediatR + FluentValidation (VSA Monolith — all in one assembly)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new global::Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "InvoiceSystem API",
        Version = "v1",
        Description = "API systemu fakturowego z pełną integracją KSeF i obsługą VSA"
    });
});

builder.Services.AddControllersWithViews(options =>
{
    // Globalny wymóg autoryzacji (wszystko chronione domyślnie)
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));

    // Spolszczenie komunikatów błędu bindowania modeli
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        _ => "Pole jest wymagane.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(
        val => $"Wartość '{val}' jest nieprawidłowa.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
        name => $"Pole {name} musi być liczbą.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
        (val, name) => $"Wartość '{val}' jest nieprawidłowa.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(
        () => "Wartość jest wymagana.");
});

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/auth/login";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "invoice-auth";

    // Obsługa API, Swaggera i AJAX - zwracanie 401/403 zamiast przekierowania do HTML logowania
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") || 
            context.Request.Path.StartsWithSegments("/ksef") ||
            context.Request.Path.StartsWithSegments("/invoices/import") ||
            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
            context.Request.Headers["Accept"].ToString().Contains("application/json") ||
            context.Request.Headers["Accept"].ToString().Contains("text/html") && context.Request.Headers["Referer"].ToString().Contains("/swagger"))
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=InvoiceSystem.db"));

builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new FeatureViewLocationExpander());
});

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "pl-PL", "en-US" };
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

builder.Services.Configure<StorageSettings>(options => {
    var configPath = builder.Configuration.GetSection("Storage").GetValue<string>("RootPath") ?? "App_Data/Storage/Incoming";
    options.RootPath = Path.Combine(builder.Environment.ContentRootPath, configPath);
});
builder.Services.Configure<InvoiceSystem.Web.Infrastructure.Configuration.AiSettings>(
    builder.Configuration.GetSection(InvoiceSystem.Web.Infrastructure.Configuration.AiSettings.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IKsefClient, KsefClient>();
builder.Services.AddHostedService<KsefSyncBackgroundService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFileHashService, FileHashService>();
builder.Services.AddScoped<IDocumentOcrService, DocumentOcrService>();
builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<IInvoicePreviewService, InvoicePreviewService>();

var app = builder.Build();

// Localization middleware
app.UseRequestLocalization();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    
    await context.Database.MigrateAsync();
    await DataSeeder.SeedAsync(context);
    await DataSeeder.SeedUsersAsync(userManager);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "InvoiceSystem API v1");
    });
}
else
{
    app.UseExceptionHandler("/invoices");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/dashboard"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run();
