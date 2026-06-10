using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;

namespace AIInterviewCoach.API.RateLimiting
{
    public static class RateLimitingPolicies
    {
        public const string AuthLogin = "AuthLogin";
        public const string InterviewTokenAccess = "InterviewTokenAccess";
        public const string CandidateSubmissionFlow = "CandidateSubmissionFlow";
        public const string AdminMutation = "AdminMutation";

        public static void Register(RateLimiterOptions options, IHostEnvironment environment)
        {
            var isIntegrationTesting = environment.IsEnvironment("IntegrationTesting");
            var authPermitLimit = isIntegrationTesting ? 50 : 5;

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString();
                }

                context.HttpContext.Response.ContentType = "application/json";

                var response = JsonSerializer.Serialize(new
                {
                    message = "Too many requests. Please wait a moment and try again.",
                    statusCode = StatusCodes.Status429TooManyRequests
                });

                await context.HttpContext.Response.WriteAsync(response, cancellationToken);
            };

            options.AddPolicy(AuthLogin, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    BuildIpPartitionKey(httpContext, "auth"),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(InterviewTokenAccess, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    BuildAuthenticatedPartitionKey(httpContext, "interview-token"),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(CandidateSubmissionFlow, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    BuildAuthenticatedPartitionKey(httpContext, "candidate-flow"),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 15,
                        TokensPerPeriod = 15,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(AdminMutation, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    BuildAuthenticatedPartitionKey(httpContext, "admin-mutation"),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 12,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        }

        private static string BuildAuthenticatedPartitionKey(HttpContext context, string prefix)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst(ClaimTypes.Name)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"{prefix}:user:{userId}";
            }

            return BuildIpPartitionKey(context, prefix);
        }

        private static string BuildIpPartitionKey(HttpContext context, string prefix)
        {
            var remoteIp = context.Connection.RemoteIpAddress?.MapToIPv6().ToString()
                ?? IPAddress.None.MapToIPv6().ToString();

            return $"{prefix}:ip:{remoteIp}";
        }
    }
}
