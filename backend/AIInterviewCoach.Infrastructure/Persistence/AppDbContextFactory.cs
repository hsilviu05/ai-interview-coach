using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AIInterviewCoach.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var apiProjectPath = ResolveApiProjectPath();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .AddJsonFile("appsettings.Development.Local.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=ai_interview_coach;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }

        private static string ResolveApiProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var candidatePaths = new[]
            {
                Path.Combine(currentDirectory, "backend", "AIInterviewCoach.API"),
                Path.Combine(currentDirectory, "..", "AIInterviewCoach.API"),
                Path.Combine(currentDirectory, "..", "backend", "AIInterviewCoach.API"),
                Path.Combine(currentDirectory, "..", "..", "backend", "AIInterviewCoach.API")
            };

            foreach (var candidatePath in candidatePaths.Select(Path.GetFullPath))
            {
                if (File.Exists(Path.Combine(candidatePath, "appsettings.json")))
                {
                    return candidatePath;
                }
            }

            return currentDirectory;
        }
    }
}
