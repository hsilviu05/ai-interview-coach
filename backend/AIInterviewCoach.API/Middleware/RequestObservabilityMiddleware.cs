using System.Diagnostics;
using AIInterviewCoach.Application.Interfaces.Services;
using Microsoft.AspNetCore.Routing;

namespace AIInterviewCoach.API.Middleware
{
    public sealed class RequestObservabilityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestObservabilityMiddleware> _logger;
        private readonly IObservabilityService _observabilityService;

        public RequestObservabilityMiddleware(
            RequestDelegate next,
            ILogger<RequestObservabilityMiddleware> logger,
            IObservabilityService observabilityService)
        {
            _next = next;
            _logger = logger;
            _observabilityService = observabilityService;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            var route = ResolveRoute(context);
            var statusCode = context.Response.StatusCode;

            _observabilityService.RecordRequest(
                context.Request.Method,
                route,
                statusCode,
                stopwatch.Elapsed);

            if (context.Request.Path.StartsWithSegments("/health"))
            {
                _logger.LogDebug(
                    "Health check {Method} {Route} responded {StatusCode} in {DurationMs} ms.",
                    context.Request.Method,
                    route,
                    statusCode,
                    Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2));
                return;
            }

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    "HTTP {Method} {Route} responded {StatusCode} in {DurationMs} ms. TraceId: {TraceId}",
                    context.Request.Method,
                    route,
                    statusCode,
                    Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    context.TraceIdentifier);
                return;
            }

            if (statusCode >= StatusCodes.Status400BadRequest)
            {
                _logger.LogWarning(
                    "HTTP {Method} {Route} responded {StatusCode} in {DurationMs} ms. TraceId: {TraceId}",
                    context.Request.Method,
                    route,
                    statusCode,
                    Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                    context.TraceIdentifier);
                return;
            }

            _logger.LogInformation(
                "HTTP {Method} {Route} responded {StatusCode} in {DurationMs} ms. TraceId: {TraceId}",
                context.Request.Method,
                route,
                statusCode,
                Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2),
                context.TraceIdentifier);
        }

        private static string ResolveRoute(HttpContext context)
        {
            if (context.GetEndpoint() is RouteEndpoint routeEndpoint)
            {
                return routeEndpoint.RoutePattern.RawText ?? context.Request.Path.Value ?? "/";
            }

            return context.Request.Path.Value ?? "/";
        }
    }
}
