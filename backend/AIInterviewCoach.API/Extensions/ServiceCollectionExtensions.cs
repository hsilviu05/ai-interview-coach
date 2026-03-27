using System.Text;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Infrastructure.Persistence;
using AIInterviewCoach.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProblemService, ProblemService>();
            services.AddScoped<IInterviewService, InterviewService>();
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddScoped<IPasswordHasherService, PasswordHasherService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();

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
            var key = jwtSettings["Key"]!;

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

            services.AddAuthorization();

            return services;
        }
    }
}