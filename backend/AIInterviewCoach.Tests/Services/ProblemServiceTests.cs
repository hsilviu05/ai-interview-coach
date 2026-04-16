using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Tests.Common;
using Microsoft.EntityFrameworkCore;

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

            var service = CreateService(db);

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

            var service = CreateService(db);

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

            var service = CreateService(db);

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

            var service = CreateService(db);

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

        [Fact]
        public async Task DeleteProblemAsync_ShouldThrow_WhenProblemIsAttachedToInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);

            var service = CreateService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeleteProblemAsync(problem.Id, interviewer.Id, isAdmin: false));
        }

        [Fact]
        public async Task CreateProblemAsync_ShouldWriteAdminAuditLog()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var service = CreateService(db);

            var result = await service.CreateProblemAsync(admin.Id, BuildCreateRequest());

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal(admin.Id, auditLog.AdminUserId);
            Assert.Equal("problem.created", auditLog.ActionType);
            Assert.Equal(result.Id, auditLog.TargetId);
            Assert.Contains("Created problem", auditLog.Summary);
            Assert.Contains("function", auditLog.DetailsJson);
        }

        [Fact]
        public async Task UpdateProblemAsync_ShouldWriteAdminAuditLog()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var problem = TestDataSeeder.CreateProblem(db, admin.Id, title: "Old Title");
            var service = CreateService(db);

            var updatedProblem = await service.UpdateProblemAsync(problem.Id, admin.Id, BuildUpdateRequest());

            Assert.NotNull(updatedProblem);

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal("problem.updated", auditLog.ActionType);
            Assert.Equal(problem.Id, auditLog.TargetId);
            Assert.Contains("Updated problem", auditLog.Summary);
            Assert.Contains("previousTitle", auditLog.DetailsJson);
            Assert.Contains("Old Title", auditLog.DetailsJson);
            Assert.Contains("Updated Title", auditLog.DetailsJson);
        }

        [Fact]
        public async Task AddTestCaseAsync_ShouldWriteAdminAuditLog_WhenAdminAddsTestCase()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var problem = TestDataSeeder.CreateProblem(db, admin.Id, testCaseCount: 0, title: "Arrays Warmup");
            var service = CreateService(db);

            var created = await service.AddTestCaseAsync(
                problem.Id,
                admin.Id,
                isAdmin: true,
                new CreateTestCaseRequestDto
                {
                    Input = "1 2",
                    ExpectedOutput = "3",
                    IsHidden = true,
                    OrderIndex = 3
                });

            Assert.Equal(3, created.OrderIndex);

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal("testcase.created", auditLog.ActionType);
            Assert.Equal(created.Id, auditLog.TargetId);
            Assert.Contains("Added test case #3", auditLog.Summary);
            Assert.Contains("Arrays Warmup", auditLog.Summary);
        }

        [Fact]
        public async Task DeleteProblemAsync_ShouldWriteAdminAuditLog_WhenAdminDeletesProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var problem = TestDataSeeder.CreateProblem(db, admin.Id, title: "Delete Me");
            var service = CreateService(db);

            var deleted = await service.DeleteProblemAsync(problem.Id, admin.Id, isAdmin: true);

            Assert.True(deleted);

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal("problem.deleted", auditLog.ActionType);
            Assert.Equal(problem.Id, auditLog.TargetId);
            Assert.Contains("Delete Me", auditLog.Summary);
        }

        [Fact]
        public async Task ReplaceCatalogWithStarterSetAsync_ShouldResetWorkspaceAndCreateStarterProblems()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(db, interview.Id, candidate.Id);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, session.Id, SubmissionStatus.Accepted);

            db.CandidateStatistics.Add(new CandidateStatistic
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                ProblemsSolved = 3,
                TotalSubmissions = 8,
                AccuracyRate = 67.5m,
                AverageExecutionTimeMs = 101,
                UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();

            var service = CreateService(db);

            var result = await service.ReplaceCatalogWithStarterSetAsync(interviewer.Id);

            Assert.Equal(1, result.DeletedProblemCount);
            Assert.Equal(1, result.DeletedInterviewCount);
            Assert.Equal(1, result.DeletedSubmissionCount);
            Assert.Equal(4, result.CreatedProblemCount);
            Assert.Contains("Two Sum", result.CreatedProblemTitles);

            var problems = await db.Problems
                .Include(existingProblem => existingProblem.TestCases)
                .ToListAsync();

            Assert.Equal(4, problems.Count);
            Assert.All(problems, existingProblem =>
            {
                Assert.Equal(ProblemExecutionModes.FunctionSignature, existingProblem.ExecutionMode);
                Assert.Equal(interviewer.Id, existingProblem.CreatedByUserId);
                Assert.NotEmpty(existingProblem.TestCases);
                Assert.False(string.IsNullOrWhiteSpace(existingProblem.CsharpHarnessTemplate));
                Assert.Contains("{{candidate_code}}", existingProblem.CsharpHarnessTemplate);
            });

            Assert.Empty(await db.Interviews.ToListAsync());
            Assert.Empty(await db.InterviewProblems.ToListAsync());
            Assert.Empty(await db.InterviewSessions.ToListAsync());
            Assert.Empty(await db.Submissions.ToListAsync());

            var candidateStatistic = await db.CandidateStatistics.SingleAsync();
            Assert.Equal(0, candidateStatistic.ProblemsSolved);
            Assert.Equal(0, candidateStatistic.TotalSubmissions);
            Assert.Equal(0, candidateStatistic.AccuracyRate);
            Assert.Equal(0, candidateStatistic.AverageExecutionTimeMs);

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal("catalog.replaced", auditLog.ActionType);
            Assert.Contains("starter set", auditLog.Summary);
            Assert.Contains("Two Sum", auditLog.DetailsJson);
        }

        private static ProblemService CreateService(Infrastructure.Persistence.AppDbContext db) =>
            new(db, new AdminAuditService(db));

        private static CreateProblemRequestDto BuildCreateRequest() =>
            new()
            {
                Title = "Graph Traversal",
                Description = "Traverse a graph safely.",
                Difficulty = "Medium",
                Topic = "Graphs",
                ConstraintsText = "1 <= n <= 1000",
                ExampleInput = "{\"nodes\":[]}",
                ExampleOutput = "[]",
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode = "public class Solution { public int Solve() { return 0; } }",
                PythonStarterCode = "class Solution:\n    def solve(self):\n        return 0",
                CppStarterCode = "#include <vector>\nclass Solution { public: int solve() { return 0; } };",
                CsharpHarnessTemplate = "{{candidate_code}}",
                PythonHarnessTemplate = "{{candidate_code}}",
                CppHarnessTemplate = "{{candidate_code}}",
                IsPublic = true
            };

        private static UpdateProblemRequestDto BuildUpdateRequest() =>
            new()
            {
                Title = "Updated Title",
                Description = "Updated description",
                Difficulty = "Hard",
                Topic = "Dynamic Programming",
                ConstraintsText = "1 <= n <= 5000",
                ExampleInput = "{\"items\":[1,2,3]}",
                ExampleOutput = "42",
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode = "public class Solution { public int Solve() { return 1; } }",
                PythonStarterCode = "class Solution:\n    def solve(self):\n        return 1",
                CppStarterCode = "#include <vector>\nclass Solution { public: int solve() { return 1; } };",
                CsharpHarnessTemplate = "{{candidate_code}}",
                PythonHarnessTemplate = "{{candidate_code}}",
                CppHarnessTemplate = "{{candidate_code}}",
                IsPublic = false
            };
    }
}
