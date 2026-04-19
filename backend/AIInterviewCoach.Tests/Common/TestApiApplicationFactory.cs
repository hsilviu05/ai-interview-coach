using System.Net.Http.Headers;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AIInterviewCoach.Tests.Common
{
    public sealed class TestApiApplicationFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private const string JwtKey = "integration-test-jwt-key-should-be-long-enough-1234567890";
        private const string JwtIssuer = "AIInterviewCoach";
        private const string JwtAudience = "AIInterviewCoachUsers";
        private const string BootstrapAdminEmail = "admin@test.com";
        private const string BootstrapAdminPassword = "Password123!";
        private const string BootstrapAdminFullName = "Admin Demo";
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private readonly bool _useTestAuthentication;

        public TestApiApplicationFactory(bool useTestAuthentication = true)
        {
            _useTestAuthentication = useTestAuthentication;
            Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
            Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
            Environment.SetEnvironmentVariable("BootstrapAdmin__Email", BootstrapAdminEmail);
            Environment.SetEnvironmentVariable("BootstrapAdmin__Password", BootstrapAdminPassword);
            Environment.SetEnvironmentVariable("BootstrapAdmin__FullName", BootstrapAdminFullName);
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTesting");

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience,
                    ["BootstrapAdmin:Email"] = BootstrapAdminEmail,
                    ["BootstrapAdmin:Password"] = BootstrapAdminPassword,
                    ["BootstrapAdmin:FullName"] = BootstrapAdminFullName,
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<IAppDbContext>();

                services.AddDbContext<AppDbContext>(options =>
                    options
                        .UseSqlite(_connection)
                        .ConfigureWarnings(warnings =>
                            warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

                services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

                if (_useTestAuthentication)
                {
                    services
                        .AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                            options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                        })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.SchemeName,
                            _ => { });
                }
            });
        }

        public async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
        {
            var client = CreateClient();

            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await dbContext.Users.SingleAsync(existingUser => existingUser.Email == email);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName);
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, user.Id.ToString());
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.EmailHeader, user.Email);
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.NameHeader, user.FullName);
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, user.UserRole.ToString());
            return client;
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
