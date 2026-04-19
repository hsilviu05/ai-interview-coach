using AIInterviewCoach.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AIInterviewCoach.API.Health
{
    public sealed class ApplicationDatabaseHealthCheck : IHealthCheck
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ApplicationDatabaseHealthCheck(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database is not reachable.");
            }

            return HealthCheckResult.Healthy(
                $"Database provider '{dbContext.Database.ProviderName ?? "unknown"}' is reachable.");
        }
    }
}
