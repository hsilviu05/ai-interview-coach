using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Tests.Common
{
    public sealed class FakeCodeExecutor : ICodeExecutor
    {
        private readonly Func<string, string, IEnumerable<TestCase>, ExecutionResult> _execute;

        public FakeCodeExecutor(
            Func<string, string, IEnumerable<TestCase>, ExecutionResult>? execute = null)
        {
            _execute = execute ?? DefaultExecute;
        }

        public Task<ExecutionResult> ExecuteAsync(
            Problem problem,
            string code,
            string language,
            IEnumerable<TestCase> testCases)
        {
            return Task.FromResult(_execute(code, language, testCases));
        }

        private static ExecutionResult DefaultExecute(
            string _,
            string __,
            IEnumerable<TestCase> testCases)
        {
            var totalTests = testCases.Count();

            return new ExecutionResult(
                SubmissionStatus.Accepted,
                "Accepted.",
                totalTests,
                totalTests,
                25,
                512);
        }
    }
}
