using System.Text;
using AIInterviewCoach.API.Authentication;
using AIInterviewCoach.API.Authorization;
using AIInterviewCoach.API.Health;
using AIInterviewCoach.API.RateLimiting;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Infrastructure.Configuration;
using AIInterviewCoach.Infrastructure.Persistence;
using AIInterviewCoach.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AIInterviewCoach.Application.Services;
using Microsoft.Net.Http.Headers;

namespace AIInterviewCoach.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
        {
            services.AddSingleton<IObservabilityService, ApplicationObservabilityService>();
            services.AddScoped<IAdminAuditService, AdminAuditService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProblemService, ProblemService>();
            services.AddSingleton<IProblemTemplateService, ProblemTemplateService>();
            services.AddScoped<IInterviewService, InterviewService>();
            services.AddScoped<SubmissionFeedbackProcessor>();
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddHttpClient<ISubmissionFeedbackService, SubmissionFeedbackService>();
            services.AddSingleton<SubmissionFeedbackBackgroundService>();
            services.AddSingleton<ISubmissionFeedbackQueue>(provider =>
                provider.GetRequiredService<SubmissionFeedbackBackgroundService>());
            services.AddHostedService(provider =>
                provider.GetRequiredService<SubmissionFeedbackBackgroundService>());
            services.AddScoped<IPasswordHasherService, PasswordHasherService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<ICodeExecutor, DotnetCodeExecutor>();

            return services;
        }

        public static IServiceCollection AddApplicationDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var databaseProvider = configuration["DatabaseProvider"]?.Trim();

            services.AddDbContext<AppDbContext>(options =>
            {
                if (string.Equals(databaseProvider, "sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseSqlite(connectionString);
                    return;
                }

                options.UseNpgsql(connectionString);
            });

            services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
            services.AddHealthChecks()
                .AddCheck<ApplicationDatabaseHealthCheck>("database", tags: ["ready"]);


            return services;
        }

        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = JwtSigningKeyResolver.GetRequiredSigningKey(configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(key))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (!string.IsNullOrWhiteSpace(context.Token))
                            {
                                return Task.CompletedTask;
                            }

                            if (context.Request.Cookies.TryGetValue(AuthCookieDefaults.CookieName, out var cookieToken) &&
                                !string.IsNullOrWhiteSpace(cookieToken))
                            {
                                context.Token = cookieToken;
                                return Task.CompletedTask;
                            }

                            if (context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authorizationHeader) &&
                                authorizationHeader.Count > 0)
                            {
                                var bearerValue = authorizationHeader.ToString();
                                if (bearerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                {
                                    context.Token = bearerValue["Bearer ".Length..].Trim();
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(AuthorizationPolicies.Register);

            return services;
        }

        public static IServiceCollection AddApplicationRateLimiting(
            this IServiceCollection services)
        {
            services.AddRateLimiter(RateLimitingPolicies.Register);

            return services;
        }
    }
}
