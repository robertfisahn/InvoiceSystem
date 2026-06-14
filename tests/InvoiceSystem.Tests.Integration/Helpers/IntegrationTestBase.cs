using System;
using System.Data.Common;
using System.Linq;
using InvoiceSystem.Web.Infrastructure.Database;
using InvoiceSystem.Web.Infrastructure.Soap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoapCore;
using Xunit;

namespace InvoiceSystem.Tests.Integration.Helpers
{
    public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        protected readonly WebApplicationFactory<Program> _factory;
        private readonly DbConnection _connection;
        private readonly WebApplication _soapMockServer;
        protected readonly string _soapMockServerUrl;

        public IntegrationTestBase(WebApplicationFactory<Program> factory)
        {
            // 1. Start SoapCore Mock service locally on a random port
            var builder = WebApplication.CreateBuilder();
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
            _soapMockServerUrl = _soapMockServer.Urls.First();

            // 2. Set up SQLite in-memory database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _factory = factory.WithWebHostBuilder(webBuilder =>
            {
                webBuilder.UseEnvironment("IntegrationTest");
                webBuilder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                    services.RemoveAll(typeof(AppDbContext));

                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlite(_connection);
                    });
                });
            });

            // Ensure database is created
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _soapMockServer.StopAsync().GetAwaiter().GetResult();
            _soapMockServer.DisposeAsync().GetAwaiter().GetResult();
            _connection.Close();
            _connection.Dispose();
        }
    }
}
