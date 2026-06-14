using System;
using System.Data.Common;
using InvoiceSystem.Web.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Tests.Unit.Common
{
    public class SqliteDbContextFixture : IDisposable
    {
        private readonly DbConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public SqliteDbContextFixture()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using (var context = CreateContext())
            {
                context.Database.EnsureCreated();
            }
        }

        public AppDbContext CreateContext()
        {
            return new AppDbContext(_options);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
