using AIInterviewCoach.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Tests.Common
{
    public static class TestDbContextFactory
    {
        public static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }

        public static SqliteTestContextScope CreateSqliteContext()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();

            return new SqliteTestContextScope(connection, context);
        }
    }

    public sealed class SqliteTestContextScope : IDisposable
    {
        public SqliteTestContextScope(SqliteConnection connection, AppDbContext context)
        {
            Connection = connection;
            Context = context;
        }

        public SqliteConnection Connection { get; }
        public AppDbContext Context { get; }

        public AppDbContext CreateAdditionalContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(Connection)
                .EnableSensitiveDataLogging()
                .Options;

            return new AppDbContext(options);
        }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }
    }
}
