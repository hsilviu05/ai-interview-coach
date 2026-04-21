using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Tests.Common;

namespace AIInterviewCoach.Tests.Services
{
    public class PracticeProblemHintRequestServiceTests
    {
        [Fact]
        public async Task GeneratePracticeHintAsync_ShouldReturnHint_ForAccessiblePublicProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(
                db,
                interviewer.Id,
                title: "Best Time to Buy and Sell Stock",
                isPublic: true);

            ProblemHintContextDto? capturedContext = null;
            var service = new PracticeProblemHintRequestService(
                db,
                new FakeProblemHintService(context =>
                {
                    capturedContext = context;
                    return new ProblemHintResponseDto
                    {
                        Level = context.Level,
                        Content = "Hint 2\nTrack the minimum price so far.",
                        Source = "OpenAI"
                    };
                }));

            var result = await service.GeneratePracticeHintAsync(
                problem.Id,
                candidate.Id,
                UserRole.Candidate,
                new ProblemHintRequestDto
                {
                    Level = 2,
                    Language = "python",
                    SourceCode = "class Solution:\n    pass"
                });

            Assert.Equal(2, result.Level);
            Assert.Equal("Hint 2\nTrack the minimum price so far.", result.Content);
            Assert.Equal("OpenAI", result.Source);
            Assert.NotNull(capturedContext);
            Assert.Equal(problem.Title, capturedContext!.ProblemTitle);
            Assert.Equal("python", capturedContext.Language);
            Assert.Equal("class Solution:\n    pass", capturedContext.SourceCode);
        }

        [Fact]
        public async Task GeneratePracticeHintAsync_ShouldThrow_WhenLevelIsOutsideSupportedRange()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, isPublic: true);

            var service = new PracticeProblemHintRequestService(
                db,
                new FakeProblemHintService());

            var action = () => service.GeneratePracticeHintAsync(
                problem.Id,
                candidate.Id,
                UserRole.Candidate,
                new ProblemHintRequestDto
                {
                    Level = 4,
                    Language = "csharp",
                    SourceCode = string.Empty
                });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);

            Assert.Contains("levels 1 through 3", exception.Message);
        }

        [Fact]
        public async Task GeneratePracticeHintAsync_ShouldThrow_WhenCandidateRequestsPrivateProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var privateProblem = TestDataSeeder.CreateProblem(
                db,
                interviewer.Id,
                isPublic: false);

            var service = new PracticeProblemHintRequestService(
                db,
                new FakeProblemHintService());

            var action = () => service.GeneratePracticeHintAsync(
                privateProblem.Id,
                candidate.Id,
                UserRole.Candidate,
                new ProblemHintRequestDto
                {
                    Level = 1,
                    Language = "cpp",
                    SourceCode = string.Empty
                });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
        }
    }
}
