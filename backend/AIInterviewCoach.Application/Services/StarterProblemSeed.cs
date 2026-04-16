using AIInterviewCoach.Domain.Entities;

namespace AIInterviewCoach.Application.Services
{
    internal sealed record StarterProblemSeed(
        Problem Problem,
        IReadOnlyList<TestCase> TestCases);
}
