using AIInterviewCoach.Application.DTOs.Observability;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IObservabilityService
    {
        void RecordRequest(string method, string route, int statusCode, TimeSpan duration);
        void RecordCodeExecution(string language, string executionMode, string status, TimeSpan duration, int evaluatedTests);
        void RecordAiFeedback(string status, string? source, TimeSpan duration);
        void RecordAdminAction(string actionType, string targetType);
        void RecordUnhandledException(string exceptionType);
        ObservabilitySnapshotResponseDto GetSnapshot();
    }
}
