using System.Net;
using System.Net.Http.Json;
using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Constants;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Persistence;
using AIInterviewCoach.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AIInterviewCoach.Tests.Integration
{
    public class SubmissionPipelineIntegrationTests
    {
        // ── aiFeedbackStatus transitions ──────────────────────────────────────

        [Fact]
        public async Task GetMySubmissions_ReturnsPendingFeedbackStatus_BeforeProcessing()
        {
            await using var factory = new TestApiApplicationFactory();

            var (candidate, problem) = await SeedCandidateWithProblemAsync(factory);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, null,
                SubmissionStatus.Accepted, passedTests: 1, totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);

            using var client = await factory.CreateAuthenticatedClientAsync(candidate.Email);

            var response = await client.GetAsync("/api/submissions/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var submissions = await response.Content.ReadFromJsonAsync<List<SubmissionResponseDto>>();
            Assert.NotNull(submissions);
            var single = Assert.Single(submissions!);
            Assert.Equal(SubmissionFeedbackStatuses.Pending, single.AiFeedbackStatus);
            Assert.Null(single.AiFeedback);
            Assert.Null(single.AiFeedbackSource);
        }

        [Fact]
        public async Task GetMySubmissions_ReflectsReadyFeedbackStatus_AfterProcessorRuns()
        {
            // SubmissionFeedbackService immediately returns LocalFallback in the test
            // environment because no API key is configured — no network call is made.
            await using var factory = new TestApiApplicationFactory();

            var (candidate, problem) = await SeedCandidateWithProblemAsync(factory);
            Guid submissionId;

            using (var seedScope = factory.Services.CreateScope())
            {
                var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var submission = TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, null,
                    SubmissionStatus.Accepted, passedTests: 1, totalTests: 1,
                    aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);
                submissionId = submission.Id;
            }

            using (var processScope = factory.Services.CreateScope())
            {
                var processor = processScope.ServiceProvider.GetRequiredService<SubmissionFeedbackProcessor>();
                await processor.ProcessAsync(submissionId);
            }

            using var client = await factory.CreateAuthenticatedClientAsync(candidate.Email);
            var response = await client.GetAsync("/api/submissions/me");

            var submissions = await response.Content.ReadFromJsonAsync<List<SubmissionResponseDto>>();
            Assert.NotNull(submissions);
            var result = Assert.Single(submissions!);
            Assert.Equal(SubmissionFeedbackStatuses.Ready, result.AiFeedbackStatus);
            Assert.NotNull(result.AiFeedback);
            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.AiFeedbackSource);
        }

        [Fact]
        public async Task GetMySubmissions_ReflectsFailedFeedbackStatus_AfterProcessorEncountersError()
        {
            // Confirms the Failed terminal state is persisted and readable through the API.
            await using var factory = new TestApiApplicationFactory();

            var (candidate, problem) = await SeedCandidateWithProblemAsync(factory);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, null,
                SubmissionStatus.WrongAnswer, passedTests: 0, totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Failed);

            using var client = await factory.CreateAuthenticatedClientAsync(candidate.Email);

            var response = await client.GetAsync("/api/submissions/me");

            var submissions = await response.Content.ReadFromJsonAsync<List<SubmissionResponseDto>>();
            Assert.NotNull(submissions);
            var result = Assert.Single(submissions!);
            Assert.Equal(SubmissionFeedbackStatuses.Failed, result.AiFeedbackStatus);
            Assert.Null(result.AiFeedbackSource);
        }

        // ── Partial-failure and session-state guards ───────────────────────────

        [Fact]
        public async Task SubmittingToCompletedSession_ReturnsBadRequest()
        {
            // Validates the session-state guard fires BEFORE code execution.
            // InvalidOperationException → 400 via ExceptionHandlingMiddleware.
            await using var factory = new TestApiApplicationFactory();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var interviewer = await db.Users.SingleAsync(u => u.Email == "recruiter@test.com");
            var candidate = await db.Users.SingleAsync(u => u.Email == "candidate@test.com");
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidate.Id, InterviewSessionStatus.Completed);

            using var client = await factory.CreateAuthenticatedClientAsync("candidate@test.com");

            var response = await client.PostAsJsonAsync("/api/submissions", new
            {
                problemId = problem.Id,
                interviewSessionId = session.Id,
                language = "csharp",
                sourceCode = "public class Solution { }"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SubmittingToSessionOwnedByAnotherCandidate_ReturnsNotFound()
        {
            // Confirms the session is silently "not found" rather than "forbidden"
            // so the session ID is not disclosed to unrelated candidates.
            await using var factory = new TestApiApplicationFactory();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var interviewer = await db.Users.SingleAsync(u => u.Email == "recruiter@test.com");
            var otherCandidate = await db.Users.SingleAsync(u => u.Email == "candidate@test.com");

            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            var sessionOwnedByOther = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, otherCandidate.Id, InterviewSessionStatus.InProgress);

            // admin@test.com is the impersonator — they have CandidateWorkspaceAccess
            // but the session belongs to candidate@test.com
            using var client = await factory.CreateAuthenticatedClientAsync("admin@test.com");

            var response = await client.PostAsJsonAsync("/api/submissions", new
            {
                problemId = problem.Id,
                interviewSessionId = sessionOwnedByOther.Id,
                language = "csharp",
                sourceCode = "public class Solution { }"
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static async Task<(Domain.Entities.User Candidate, Domain.Entities.Problem Problem)>
            SeedCandidateWithProblemAsync(TestApiApplicationFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var interviewer = await db.Users.SingleAsync(u => u.Email == "recruiter@test.com");
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);

            return (candidate, problem);
        }
    }
}
