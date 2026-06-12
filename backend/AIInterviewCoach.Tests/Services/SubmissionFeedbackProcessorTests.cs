using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Constants;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Services;
using AIInterviewCoach.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIInterviewCoach.Tests.Services
{
    public class SubmissionFeedbackProcessorTests
    {
        [Fact]
        public async Task ProcessAsync_ShouldStoreFeedbackAndMarkSubmissionReady()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db,
                candidate.Id,
                problem.Id,
                null,
                SubmissionStatus.Accepted,
                passedTests: 1,
                totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);

            var processor = new SubmissionFeedbackProcessor(
                db,
                new FakeSubmissionFeedbackService(_ => new SubmissionFeedbackResultDto
                {
                    Content = "Overall\nSpecific feedback",
                    Source = SubmissionFeedbackSources.OpenAI
                }),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(submission.Id);

            var storedSubmission = db.Submissions.Single(x => x.Id == submission.Id);
            Assert.Equal(SubmissionFeedbackStatuses.Ready, storedSubmission.AiFeedbackStatus);
            Assert.Equal("Overall\nSpecific feedback", storedSubmission.AiFeedback);
        }

        [Fact]
        public async Task ProcessAsync_ShouldMarkSubmissionFailed_WhenFeedbackGenerationThrows()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db,
                candidate.Id,
                problem.Id,
                null,
                SubmissionStatus.Accepted,
                passedTests: 1,
                totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);

            var processor = new SubmissionFeedbackProcessor(
                db,
                new FakeSubmissionFeedbackService(_ =>
                    throw new InvalidOperationException("AI service unavailable.")),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(submission.Id);

            var storedSubmission = db.Submissions.Single(x => x.Id == submission.Id);
            Assert.Equal(SubmissionFeedbackStatuses.Failed, storedSubmission.AiFeedbackStatus);
            Assert.Null(storedSubmission.AiFeedback);
        }

        // ── Idempotency ───────────────────────────────────────────────────────

        [Fact]
        public async Task ProcessAsync_ShouldSkipFeedbackGeneration_WhenSubmissionIsAlreadyReady()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db,
                candidate.Id,
                problem.Id,
                null,
                SubmissionStatus.Accepted,
                passedTests: 1,
                totalTests: 1,
                aiFeedback: "Existing AI feedback",
                aiFeedbackStatus: SubmissionFeedbackStatuses.Ready);

            var callCount = 0;
            var processor = new SubmissionFeedbackProcessor(
                db,
                new FakeSubmissionFeedbackService(_ =>
                {
                    callCount++;
                    return new SubmissionFeedbackResultDto { Content = "Replacement content", Source = SubmissionFeedbackSources.OpenAI };
                }),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(submission.Id);

            Assert.Equal(0, callCount);
            var stored = db.Submissions.Single(x => x.Id == submission.Id);
            Assert.Equal("Existing AI feedback", stored.AiFeedback);
            Assert.Equal(SubmissionFeedbackStatuses.Ready, stored.AiFeedbackStatus);
        }

        [Fact]
        public async Task ProcessAsync_ShouldSkipFeedbackGeneration_WhenSubmissionIsAlreadyFailed()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db,
                candidate.Id,
                problem.Id,
                null,
                SubmissionStatus.Accepted,
                passedTests: 0,
                totalTests: 1,
                aiFeedback: null,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Failed);

            var callCount = 0;
            var processor = new SubmissionFeedbackProcessor(
                db,
                new FakeSubmissionFeedbackService(_ => { callCount++; return new SubmissionFeedbackResultDto(); }),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(submission.Id);

            Assert.Equal(0, callCount);
            var stored = db.Submissions.Single(x => x.Id == submission.Id);
            Assert.Equal(SubmissionFeedbackStatuses.Failed, stored.AiFeedbackStatus);
            Assert.Null(stored.AiFeedback);
        }

        // ── Feedback context ──────────────────────────────────────────────────

        [Fact]
        public async Task ProcessAsync_TrimsLeadingAndTrailingWhitespace_FromFeedbackContent()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db, candidate.Id, problem.Id, null,
                SubmissionStatus.Accepted, passedTests: 1, totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);

            var processor = new SubmissionFeedbackProcessor(
                db,
                new FakeSubmissionFeedbackService(_ => new SubmissionFeedbackResultDto
                {
                    Content = "  \n  Overall\nLooks good.\n  ",
                    Source = SubmissionFeedbackSources.OpenAI
                }),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(submission.Id);

            var stored = db.Submissions.Single(x => x.Id == submission.Id);
            Assert.Equal("Overall\nLooks good.", stored.AiFeedback);
        }

        [Fact]
        public async Task ProcessAsync_SetsPracticeModeTrue_WhenSubmissionHasNoInterviewSession()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db, candidate.Id, problem.Id, null,
                SubmissionStatus.Accepted, passedTests: 1, totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);

            SubmissionFeedbackContextDto? capturedContext = null;
            var processor = new SubmissionFeedbackProcessor(
                db,
                new FakeSubmissionFeedbackService(ctx =>
                {
                    capturedContext = ctx;
                    return new SubmissionFeedbackResultDto
                    {
                        Content = "Overall\nOK.",
                        Source = SubmissionFeedbackSources.OpenAI
                    };
                }),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(submission.Id);

            Assert.NotNull(capturedContext);
            Assert.True(capturedContext.IsPracticeMode);
        }

        [Fact]
        public async Task ProcessAsync_SetsPracticeModeFalse_WhenSubmissionBelongsToInterviewSession()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(db, interview.Id, candidate.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var submission = TestDataSeeder.CreateSubmission(
                db, candidate.Id, problem.Id, session.Id,
                SubmissionStatus.Accepted, passedTests: 1, totalTests: 1,
                aiFeedbackStatus: SubmissionFeedbackStatuses.Pending);

            SubmissionFeedbackContextDto? capturedContext = null;
            var processor = new SubmissionFeedbackProcessor(
                db,
                new FakeSubmissionFeedbackService(ctx =>
                {
                    capturedContext = ctx;
                    return new SubmissionFeedbackResultDto
                    {
                        Content = "Overall\nOK.",
                        Source = SubmissionFeedbackSources.OpenAI
                    };
                }),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(submission.Id);

            Assert.NotNull(capturedContext);
            Assert.False(capturedContext.IsPracticeMode);
        }

        // ── Submission no longer exists ───────────────────────────────────────

        [Fact]
        public async Task ProcessAsync_ShouldSkipSilently_WhenSubmissionHasBeenDeleted()
        {
            // Documents both the "submission not found" early return AND a subtle
            // design quirk: the `if (submission.Problem is null)` branch inside
            // ProcessAsync is dead code via the normal Include query.
            //
            // Reason: EF Core treats Problem as a *required* navigation (non-
            // nullable type). Include() uses INNER JOIN semantics, so if the
            // Problem row is deleted, EF excludes the Submission from results
            // entirely and FirstOrDefaultAsync returns null — the code hits the
            // `submission is null` guard instead and returns without changing
            // AiFeedbackStatus to Failed. The Pending submission is therefore
            // silently abandoned, which may be worth revisiting.
            using var db = TestDbContextFactory.CreateContext();

            var nonExistentId = Guid.NewGuid();
            var callCount = 0;
            var processor = new SubmissionFeedbackProcessor(
                db,
                new FakeSubmissionFeedbackService(_ => { callCount++; return new SubmissionFeedbackResultDto(); }),
                new ApplicationObservabilityService(),
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(nonExistentId);

            Assert.Equal(0, callCount);
        }
    }
}
