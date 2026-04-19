using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AIInterviewCoach.API.Health
{
    public static class HealthResponseWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static Task WriteAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
                traceId = context.TraceIdentifier,
                generatedAtUtc = DateTime.UtcNow,
                checks = report.Entries
                    .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => new
                        {
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description,
                            durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
                        },
                        StringComparer.OrdinalIgnoreCase)
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }
}
