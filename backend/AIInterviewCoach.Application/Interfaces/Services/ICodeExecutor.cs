using AIInterviewCoach.Domain.Entities;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface ICodeExecutor
    {
        Task<ExecutionResult> ExecuteAsync(string code, string language, IEnumerable<TestCase> testCases);
    }

    public record ExecutionResult(
    bool Success, 
    string Output, 
    int PassedTests, 
    int TotalTests, 
    int? TimeMs, 
    int? MemoryKb);
}