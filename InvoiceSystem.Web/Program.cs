using InvoiceSystem.Web.Modules.Auth.Domain;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Behaviors;
using InvoiceSystem.Web.Setup;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using InvoiceSystem.Web.Modules.Auth;
using InvoiceSystem.Web.Modules.Contractors;
using InvoiceSystem.Web.Modules.Dashboard;
using InvoiceSystem.Web.Modules.Gus;
using InvoiceSystem.Web.Modules.Invoices;
using InvoiceSystem.Web.Modules.Ksef;
using SoapCore;
using InvoiceSystem.Web.Infrastructure.Errors;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Exception Handling & Problem Details
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=InvoiceSystem.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

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

// General Framework Services
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSoapCore();

// (Modular Monolith DEPENDENCY INJECTION)
builder.Services.AddAuthModule();
builder.Services.AddContractorsModule();
builder.Services.AddDashboardModule();
builder.Services.AddGusModule();
builder.Services.AddInvoicesModule(builder.Configuration);
builder.Services.AddKsefModule(builder.Configuration);

var app = builder.Build();

// Localization middleware
app.UseRequestLocalization();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    
    if (!app.Environment.IsEnvironment("IntegrationTest"))
    {
        await context.Database.MigrateAsync();
        await DataSeeder.SeedAsync(context);
        await DataSeeder.SeedUsersAsync(userManager);
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/error");

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
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Module specific pipeline maps
app.UseGusModule();
app.MapKsefEndpoints();

// Global routing
app.MapGet("/", () => Results.Redirect("/dashboard"));
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
