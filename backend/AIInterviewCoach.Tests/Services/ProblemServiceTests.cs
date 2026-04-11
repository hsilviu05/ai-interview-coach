using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Tests.Common;

namespace AIInterviewCoach.Tests.Services
{
    public class ProblemServiceTests
    {
        [Fact]
        public async Task GetAllProblemsAsync_ShouldOnlyReturnPublicProblemsForCandidates()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var publicProblem = TestDataSeeder.CreateProblem(db, interviewer.Id, title: "Public", isPublic: true);
            TestDataSeeder.CreateProblem(db, interviewer.Id, title: "Private", isPublic: false);

            var service = new ProblemService(db);

            var results = (await service.GetAllProblemsAsync(candidate.Id, UserRole.Candidate)).ToList();

            Assert.Single(results);
            Assert.Equal(publicProblem.Id, results[0].Id);
        }

        [Fact]
        public async Task GetProblemByIdAsync_ShouldThrow_WhenCandidateRequestsPrivateProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var privateProblem = TestDataSeeder.CreateProblem(db, interviewer.Id, isPublic: false);

            var service = new ProblemService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetProblemByIdAsync(privateProblem.Id, candidate.Id, UserRole.Candidate));
        }

        [Fact]
        public async Task GetTestCasesAsync_ShouldThrow_WhenInterviewerDoesNotOwnProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var otherInterviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id, isPublic: false);

            var service = new ProblemService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetTestCasesAsync(problem.Id, otherInterviewer.Id, isAdmin: false, includeHidden: true));
        }

        [Fact]
        public async Task AddTestCaseAsync_ShouldThrow_WhenInterviewerDoesNotOwnProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var otherInterviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id, isPublic: false);

            var service = new ProblemService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.AddTestCaseAsync(
                    problem.Id,
                    otherInterviewer.Id,
                    isAdmin: false,
                    new CreateTestCaseRequestDto
                    {
                        Input = "1 2",
                        ExpectedOutput = "3",
                        IsHidden = true,
                        OrderIndex = 99
                    }));
        }
    }
}
