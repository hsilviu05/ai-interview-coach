using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface ICodeExecutor
    {
        Task<ExecutionResult> ExecuteAsync(string code, string language, IEnumerable<TestCase> testCases);
    }

    public record ExecutionResult(
        SubmissionStatus Status,
        string Output,
        int PassedTests,
        int TotalTests,
        int? TimeMs,
        int? MemoryKb);
}
