namespace AIInterviewCoach.Application.DTOs.Observability
{
    public sealed class ObservabilitySnapshotResponseDto
    {
        public DateTime GeneratedAtUtc { get; init; }
        public double UptimeSeconds { get; init; }
        public RequestMetricsSnapshotDto Requests { get; init; } = new();
        public CodeExecutionMetricsSnapshotDto CodeExecution { get; init; } = new();
        public AiFeedbackMetricsSnapshotDto AiFeedback { get; init; } = new();
        public AdminActionMetricsSnapshotDto AdminActions { get; init; } = new();
        public ExceptionMetricsSnapshotDto Exceptions { get; init; } = new();
    }

    public sealed class RequestMetricsSnapshotDto
    {
        public long TotalRequests { get; init; }
        public long FailedRequests { get; init; }
        public double AverageDurationMs { get; init; }
        public Dictionary<string, long> ByMethod { get; init; } = [];
        public Dictionary<string, long> ByRoute { get; init; } = [];
        public Dictionary<string, long> ByStatusCode { get; init; } = [];
    }

    public sealed class CodeExecutionMetricsSnapshotDto
    {
        public long TotalExecutions { get; init; }
        public double AverageDurationMs { get; init; }
        public long TotalEvaluatedTests { get; init; }
        public Dictionary<string, long> ByLanguage { get; init; } = [];
        public Dictionary<string, long> ByExecutionMode { get; init; } = [];
        public Dictionary<string, long> ByStatus { get; init; } = [];
    }

    public sealed class AiFeedbackMetricsSnapshotDto
    {
        public long TotalJobs { get; init; }
        public double AverageDurationMs { get; init; }
        public Dictionary<string, long> ByStatus { get; init; } = [];
        public Dictionary<string, long> BySource { get; init; } = [];
    }

    public sealed class AdminActionMetricsSnapshotDto
    {
        public long TotalActions { get; init; }
        public Dictionary<string, long> ByActionType { get; init; } = [];
        public Dictionary<string, long> ByTargetType { get; init; } = [];
    }

    public sealed class ExceptionMetricsSnapshotDto
    {
        public long TotalUnhandledExceptions { get; init; }
        public Dictionary<string, long> ByType { get; init; } = [];
    }
}
