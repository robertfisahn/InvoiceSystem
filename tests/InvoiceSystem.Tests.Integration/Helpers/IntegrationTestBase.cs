using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Modules.Ksef.Infrastructure;
using InvoiceSystem.Web.Modules.Gus.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoapCore;
using Testcontainers.MsSql;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Helpers
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthenticationScheme = "TestScheme";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Request.Headers.ContainsKey("X-Skip-Test-Auth"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[] { 
                new Claim(ClaimTypes.Name, "TestUser"), 
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class TestKsefClient : IKsefClient
    {
        public bool ShouldThrow { get; set; }
        public string ErrorMessage { get; set; } = "KSeF connection failed.";
        public Exception? ExceptionToThrow { get; set; }
        public Exception? InitSessionExceptionToThrow { get; set; }
        public Exception? SendInvoiceExceptionToThrow { get; set; }
        public Exception? GetInvoiceStatusExceptionToThrow { get; set; }
        public Exception? DownloadUpoExceptionToThrow { get; set; }
        public Exception? CloseSessionExceptionToThrow { get; set; }
        public Exception? DownloadXmlExceptionToThrow { get; set; }
        public string StatusToReturn { get; set; } = "Processed";
        public string KsefNumberToReturn { get; set; } = "2222222222-20260615-123456-ABCDEF";
        public List<KsefIncomingInvoiceDto> IncomingInvoicesToReturn { get; set; } = new();
        public string XmlToReturn { get; set; } = "<InvoiceXml/>";

        public Task<KsefChallengeResult> AuthorisationChallengeAsync(string nip, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (ShouldThrow) throw new Exception(ErrorMessage);
            return Task.FromResult(new KsefChallengeResult("mock_challenge_123", "2026-06-15T12:00:00Z"));
        }

        public Task<string> InitSessionAsync(string nip, string apiKey, string challenge, string timestamp, CancellationToken cancellationToken = default)
        {
            if (InitSessionExceptionToThrow != null) throw InitSessionExceptionToThrow;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (ShouldThrow) throw new Exception(ErrorMessage);
            return Task.FromResult("mock_token|ref|aes|iv|online_ref_999");
        }

        public Task CloseSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
        {
            if (CloseSessionExceptionToThrow != null) throw CloseSessionExceptionToThrow;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (ShouldThrow) throw new Exception(ErrorMessage);
            return Task.CompletedTask;
        }

        public Task<string> SendInvoiceAsync(string sessionToken, string invoiceXml, CancellationToken cancellationToken = default)
        {
            if (SendInvoiceExceptionToThrow != null) throw SendInvoiceExceptionToThrow;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (ShouldThrow) throw new Exception(ErrorMessage);
            return Task.FromResult("transaction_id_888");
        }

        public Task<KsefStatusResult> GetInvoiceStatusAsync(string sessionToken, string transactionId, CancellationToken cancellationToken = default)
        {
            if (GetInvoiceStatusExceptionToThrow != null) throw GetInvoiceStatusExceptionToThrow;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (ShouldThrow) throw new Exception(ErrorMessage);
            return Task.FromResult(new KsefStatusResult(StatusToReturn, KsefNumberToReturn, StatusToReturn == "Failed" ? ErrorMessage : null));
        }

        public Task<List<KsefIncomingInvoiceDto>> SyncInvoicesAsync(string sessionToken, DateTime fromDate, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (ShouldThrow) throw new Exception(ErrorMessage);
            return Task.FromResult(IncomingInvoicesToReturn);
        }

        public Task<string> DownloadInvoiceXmlAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default)
        {
            if (DownloadXmlExceptionToThrow != null) throw DownloadXmlExceptionToThrow;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (ShouldThrow) throw new Exception(ErrorMessage);
            return Task.FromResult(XmlToReturn);
        }

        public Task<string> DownloadUpoAsync(string sessionToken, string ksefNumber, CancellationToken cancellationToken = default)
        {
            if (DownloadUpoExceptionToThrow != null) throw DownloadUpoExceptionToThrow;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (ShouldThrow) throw new Exception(ErrorMessage);
            return Task.FromResult("<UpoXml/>");
        }
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly bool _isSqlServer;
        private readonly string _connectionString;
        private readonly SqliteConnection? _sqliteConnection;
        private readonly WebApplication _soapMockServer;
        public string SoapMockServerUrl { get; }

        public CustomWebApplicationFactory(bool isSqlServer, string connectionString, SqliteConnection? sqliteConnection = null)
        {
            _isSqlServer = isSqlServer;
            _connectionString = connectionString;
            _sqliteConnection = sqliteConnection;

            // Start SoapCore Mock service locally on a random port
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddSoapCore();
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddScoped<IUslugaBIRzewnPubl, UslugaBIRzewnPublMock>();

            _soapMockServer = builder.Build();
            ((IApplicationBuilder)_soapMockServer).UseSoapEndpoint<IUslugaBIRzewnPubl>(
                "/services/regon.asmx", 
                new SoapEncoderOptions(), 
                SoapSerializer.DataContractSerializer
            );

            _soapMockServer.StartAsync().GetAwaiter().GetResult();
            SoapMockServerUrl = _soapMockServer.Urls.First();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTest");

            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    { "DatabaseProvider", _isSqlServer ? "SqlServer" : "Sqlite" },
                    { "ConnectionStrings:DefaultConnection", _connectionString }
                });
            });

            builder.ConfigureServices(services =>
            {
                // Register and enforce custom Test Authentication Scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });

                // Register TestKsefClient replacing the real KsefClient
                services.RemoveAll(typeof(IKsefClient));
                services.AddSingleton<IKsefClient, TestKsefClient>();

                // Mock/Substitute IDocumentOcrService to avoid external downloads and Tesseract dependency
                services.RemoveAll(typeof(InvoiceSystem.Web.Modules.Invoices.Infrastructure.Ocr.IDocumentOcrService));
                services.AddSingleton<InvoiceSystem.Web.Modules.Invoices.Infrastructure.Ocr.IDocumentOcrService>(sp => NSubstitute.Substitute.For<InvoiceSystem.Web.Modules.Invoices.Infrastructure.Ocr.IDocumentOcrService>());

                // Mock/Substitute ILlmService to avoid external OpenAI/Gemini API calls
                services.RemoveAll(typeof(InvoiceSystem.Web.Modules.Invoices.Infrastructure.Llm.ILlmService));
                services.AddSingleton<InvoiceSystem.Web.Modules.Invoices.Infrastructure.Llm.ILlmService>(sp => NSubstitute.Substitute.For<InvoiceSystem.Web.Modules.Invoices.Infrastructure.Llm.ILlmService>());

                if (!_isSqlServer)
                {
                    services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                    services.RemoveAll(typeof(AppDbContext));

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlite(_sqliteConnection!);
                    });
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _soapMockServer.StopAsync().GetAwaiter().GetResult();
                _soapMockServer.DisposeAsync().GetAwaiter().GetResult();
            }
            base.Dispose(disposing);
        }
    }

    public class IntegrationTestBase : IDisposable
    {
        private static readonly object _containerLock = new object();
        private static readonly object _hostBuildLock = new object();
        private static MsSqlContainer? _sharedDbContainer;
        private static string? _sharedConnString;

        protected readonly CustomWebApplicationFactory _factory;
        private readonly SqliteConnection? _connection;
        protected readonly string _soapMockServerUrl;

        public IntegrationTestBase()
        {
            // 1. Locate the solution root containing InvoiceSystem.sln
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !dir.GetFiles("InvoiceSystem.sln").Any())
            {
                dir = dir.Parent;
            }
            if (dir == null)
            {
                throw new DirectoryNotFoundException("Could not find solution root containing InvoiceSystem.sln");
            }
            var webProjectDir = Path.Combine(dir.FullName, "InvoiceSystem.Web");

            // 2. Load configuration from Web project's .env and appsettings.json
            var envPath = Path.Combine(webProjectDir, ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(webProjectDir)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Integration tests should default to SQL Server (via Testcontainers) to match production,
            // regardless of the local SQLite development settings.
            var dbProvider = "SqlServer";
            var connString = config.GetConnectionString("DefaultConnection");

            string finalConnString = connString ?? "Data Source=InvoiceSystem.db";
            bool isSqlServer = dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase);
            bool useTestcontainers = isSqlServer && (string.IsNullOrEmpty(connString) || connString.Contains("InvoiceSystem.db") || connString.Contains(":memory:"));

            if (useTestcontainers)
            {
                if (_sharedConnString == null)
                {
                    lock (_containerLock)
                    {
                        if (_sharedConnString == null)
                        {
                            try
                            {
                                // Spin up one shared MS SQL Server Testcontainer for the entire suite
                                _sharedDbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                                    .Build();
                                _sharedDbContainer.StartAsync().GetAwaiter().GetResult();
                                _sharedConnString = _sharedDbContainer.GetConnectionString();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[WARNING] Failed to start MS SQL Testcontainer: {ex.Message}. Falling back to SQLite in-memory.");
                                _sharedConnString = "fallback-sqlite";
                            }
                        }
                    }
                }

                if (_sharedConnString == "fallback-sqlite")
                {
                    isSqlServer = false;
                    _connection = new SqliteConnection("DataSource=:memory:");
                    _connection.Open();
                    finalConnString = "DataSource=:memory:";
                }
                else
                {
                    // Generate a unique database name on the shared container for this test instance
                    var dbName = $"TestDb_{Guid.NewGuid():N}";
                    var connBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_sharedConnString)
                    {
                        InitialCatalog = dbName
                    };
                    finalConnString = connBuilder.ConnectionString;
                }
            }
            else if (!isSqlServer)
            {
                // Fallback to SQLite (either in-memory or custom configured)
                var sqliteConnString = string.IsNullOrEmpty(connString) || connString.Contains("InvoiceSystem.db") ? "DataSource=:memory:" : connString;
                _connection = new SqliteConnection(sqliteConnString);
                _connection.Open();
                finalConnString = sqliteConnString;
            }

            // Create a completely new factory instance for this test case
            _factory = new CustomWebApplicationFactory(isSqlServer, finalConnString, _connection);
            _soapMockServerUrl = _factory.SoapMockServerUrl;

            // Synchronize host builder execution to avoid race conditions with process-wide env variables
            lock (_hostBuildLock)
            {
                var prevProvider = Environment.GetEnvironmentVariable("DatabaseProvider");
                var prevConnString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

                Environment.SetEnvironmentVariable("DatabaseProvider", isSqlServer ? "SqlServer" : "Sqlite");
                Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", finalConnString);

                try
                {
                    // Accessing Services forces host creation and triggers ConfigureWebHost
                    using var scope = _factory.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();
                }
                finally
                {
                    Environment.SetEnvironmentVariable("DatabaseProvider", prevProvider);
                    Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", prevConnString);
                }
            }
        }

        public void Dispose()
        {
            _factory.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
