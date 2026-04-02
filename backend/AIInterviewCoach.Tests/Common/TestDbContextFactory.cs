using AIInterviewCoach.Infrastructure.Persistence;
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
    }
}