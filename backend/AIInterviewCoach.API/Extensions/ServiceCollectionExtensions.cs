using System.Text;
using AIInterviewCoach.API.Authorization;
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

namespace AIInterviewCoach.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
        {
            services.AddScoped<IAdminAuditService, AdminAuditService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProblemService, ProblemService>();
            services.AddSingleton<IProblemTemplateService, ProblemTemplateService>();
            services.AddScoped<IInterviewService, InterviewService>();
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddHttpClient<ISubmissionFeedbackService, SubmissionFeedbackService>();
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

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());


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
