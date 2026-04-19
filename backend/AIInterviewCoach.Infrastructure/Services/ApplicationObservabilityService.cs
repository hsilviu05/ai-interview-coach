using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using AIInterviewCoach.Application.DTOs.Observability;
using AIInterviewCoach.Application.Interfaces.Services;

namespace AIInterviewCoach.Infrastructure.Services
{
    public sealed class ApplicationObservabilityService : IObservabilityService
    {
        private static readonly Meter Meter = new("AIInterviewCoach.Backend", "1.0.0");

        private readonly Counter<long> _requestCounter = Meter.CreateCounter<long>("aic_http_requests_total");
        private readonly Histogram<double> _requestDuration = Meter.CreateHistogram<double>("aic_http_request_duration_ms");
        private readonly Counter<long> _codeExecutionCounter = Meter.CreateCounter<long>("aic_code_executions_total");
        private readonly Histogram<double> _codeExecutionDuration = Meter.CreateHistogram<double>("aic_code_execution_duration_ms");
        private readonly Counter<long> _aiFeedbackCounter = Meter.CreateCounter<long>("aic_ai_feedback_jobs_total");
        private readonly Histogram<double> _aiFeedbackDuration = Meter.CreateHistogram<double>("aic_ai_feedback_duration_ms");
        private readonly Counter<long> _adminActionCounter = Meter.CreateCounter<long>("aic_admin_actions_total");
        private readonly Counter<long> _unhandledExceptionCounter = Meter.CreateCounter<long>("aic_unhandled_exceptions_total");

        private readonly DateTime _startedAtUtc = DateTime.UtcNow;

        private readonly ConcurrentDictionary<string, long> _requestsByMethod = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _requestsByRoute = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _requestsByStatusCode = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, long> _codeExecutionsByLanguage = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _codeExecutionsByExecutionMode = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _codeExecutionsByStatus = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _aiFeedbackByStatus = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _aiFeedbackBySource = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _adminActionsByType = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _adminActionsByTargetType = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _exceptionsByType = new(StringComparer.OrdinalIgnoreCase);

        private long _totalRequests;
        private long _failedRequests;
        private long _totalRequestDurationTicks;
        private long _totalCodeExecutions;
        private long _totalCodeExecutionDurationTicks;
        private long _totalEvaluatedTests;
        private long _totalAiFeedbackJobs;
        private long _totalAiFeedbackDurationTicks;
        private long _totalAdminActions;
        private long _totalUnhandledExceptions;

        public void RecordRequest(string method, string route, int statusCode, TimeSpan duration)
        {
            var normalizedMethod = Normalize(method, "UNKNOWN");
            var normalizedRoute = Normalize(route, "unknown");
            var normalizedStatusCode = statusCode.ToString();

            Interlocked.Increment(ref _totalRequests);
            Interlocked.Add(ref _totalRequestDurationTicks, duration.Ticks);

            if (statusCode >= 500)
            {
                Interlocked.Increment(ref _failedRequests);
            }

            Increment(_requestsByMethod, normalizedMethod);
            Increment(_requestsByRoute, normalizedRoute);
            Increment(_requestsByStatusCode, normalizedStatusCode);

            var tags = new TagList
            {
                { "method", normalizedMethod },
                { "route", normalizedRoute },
                { "status_code", normalizedStatusCode }
            };

            _requestCounter.Add(1, tags);
            _requestDuration.Record(duration.TotalMilliseconds, tags);
        }

        public void RecordCodeExecution(
            string language,
            string executionMode,
            string status,
            TimeSpan duration,
            int evaluatedTests)
        {
            var normalizedLanguage = Normalize(language, "unknown");
            var normalizedExecutionMode = Normalize(executionMode, "unknown");
            var normalizedStatus = Normalize(status, "unknown");

            Interlocked.Increment(ref _totalCodeExecutions);
            Interlocked.Add(ref _totalCodeExecutionDurationTicks, duration.Ticks);
            Interlocked.Add(ref _totalEvaluatedTests, Math.Max(0, evaluatedTests));

            Increment(_codeExecutionsByLanguage, normalizedLanguage);
            Increment(_codeExecutionsByExecutionMode, normalizedExecutionMode);
            Increment(_codeExecutionsByStatus, normalizedStatus);

            var tags = new TagList
            {
                { "language", normalizedLanguage },
                { "execution_mode", normalizedExecutionMode },
                { "status", normalizedStatus }
            };

            _codeExecutionCounter.Add(1, tags);
            _codeExecutionDuration.Record(duration.TotalMilliseconds, tags);
        }

        public void RecordAiFeedback(string status, string? source, TimeSpan duration)
        {
            var normalizedStatus = Normalize(status, "unknown");
            var normalizedSource = Normalize(source, "unknown");

            Interlocked.Increment(ref _totalAiFeedbackJobs);
            Interlocked.Add(ref _totalAiFeedbackDurationTicks, duration.Ticks);

            Increment(_aiFeedbackByStatus, normalizedStatus);
            Increment(_aiFeedbackBySource, normalizedSource);

            var tags = new TagList
            {
                { "status", normalizedStatus },
                { "source", normalizedSource }
            };

            _aiFeedbackCounter.Add(1, tags);
            _aiFeedbackDuration.Record(duration.TotalMilliseconds, tags);
        }

        public void RecordAdminAction(string actionType, string targetType)
        {
            var normalizedActionType = Normalize(actionType, "unknown");
            var normalizedTargetType = Normalize(targetType, "unknown");

            Interlocked.Increment(ref _totalAdminActions);
            Increment(_adminActionsByType, normalizedActionType);
            Increment(_adminActionsByTargetType, normalizedTargetType);

            var tags = new TagList
            {
                { "action_type", normalizedActionType },
                { "target_type", normalizedTargetType }
            };

            _adminActionCounter.Add(1, tags);
        }

        public void RecordUnhandledException(string exceptionType)
        {
            var normalizedExceptionType = Normalize(exceptionType, "Exception");

            Interlocked.Increment(ref _totalUnhandledExceptions);
            Increment(_exceptionsByType, normalizedExceptionType);
            _unhandledExceptionCounter.Add(1, new TagList { { "exception_type", normalizedExceptionType } });
        }

        public ObservabilitySnapshotResponseDto GetSnapshot()
        {
            var now = DateTime.UtcNow;

            return new ObservabilitySnapshotResponseDto
            {
                GeneratedAtUtc = now,
                UptimeSeconds = Math.Round((now - _startedAtUtc).TotalSeconds, 2),
                Requests = new RequestMetricsSnapshotDto
                {
                    TotalRequests = Interlocked.Read(ref _totalRequests),
                    FailedRequests = Interlocked.Read(ref _failedRequests),
                    AverageDurationMs = CalculateAverageMilliseconds(
                        Interlocked.Read(ref _totalRequests),
                        Interlocked.Read(ref _totalRequestDurationTicks)),
                    ByMethod = Snapshot(_requestsByMethod),
                    ByRoute = Snapshot(_requestsByRoute),
                    ByStatusCode = Snapshot(_requestsByStatusCode)
                },
                CodeExecution = new CodeExecutionMetricsSnapshotDto
                {
                    TotalExecutions = Interlocked.Read(ref _totalCodeExecutions),
                    AverageDurationMs = CalculateAverageMilliseconds(
                        Interlocked.Read(ref _totalCodeExecutions),
                        Interlocked.Read(ref _totalCodeExecutionDurationTicks)),
                    TotalEvaluatedTests = Interlocked.Read(ref _totalEvaluatedTests),
                    ByLanguage = Snapshot(_codeExecutionsByLanguage),
                    ByExecutionMode = Snapshot(_codeExecutionsByExecutionMode),
                    ByStatus = Snapshot(_codeExecutionsByStatus)
                },
                AiFeedback = new AiFeedbackMetricsSnapshotDto
                {
                    TotalJobs = Interlocked.Read(ref _totalAiFeedbackJobs),
                    AverageDurationMs = CalculateAverageMilliseconds(
                        Interlocked.Read(ref _totalAiFeedbackJobs),
                        Interlocked.Read(ref _totalAiFeedbackDurationTicks)),
                    ByStatus = Snapshot(_aiFeedbackByStatus),
                    BySource = Snapshot(_aiFeedbackBySource)
                },
                AdminActions = new AdminActionMetricsSnapshotDto
                {
                    TotalActions = Interlocked.Read(ref _totalAdminActions),
                    ByActionType = Snapshot(_adminActionsByType),
                    ByTargetType = Snapshot(_adminActionsByTargetType)
                },
                Exceptions = new ExceptionMetricsSnapshotDto
                {
                    TotalUnhandledExceptions = Interlocked.Read(ref _totalUnhandledExceptions),
                    ByType = Snapshot(_exceptionsByType)
                }
            };
        }

        private static void Increment(ConcurrentDictionary<string, long> dictionary, string key)
        {
            dictionary.AddOrUpdate(key, 1, (_, current) => current + 1);
        }

        private static string Normalize(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();
        }

        private static double CalculateAverageMilliseconds(long totalCount, long totalTicks)
        {
            if (totalCount <= 0 || totalTicks <= 0)
            {
                return 0;
            }

            return Math.Round(TimeSpan.FromTicks(totalTicks).TotalMilliseconds / totalCount, 2);
        }

        private static Dictionary<string, long> Snapshot(ConcurrentDictionary<string, long> source)
        {
            return source
                .OrderByDescending(entry => entry.Value)
                .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
