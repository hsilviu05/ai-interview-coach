using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Constants;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIInterviewCoach.Tests.Services
{
    public class SubmissionServiceTests
    {
        [Fact]
        public async Task CreateSubmissionAsync_ShouldCreateSubmission_WhenProblemBelongsToActiveInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 2);

            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, points: 100, orderIndex: 1);

            var session = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.InProgress);
            var feedbackQueue = new FakeSubmissionFeedbackQueue();

            var service = new SubmissionService(
                db,
                new FakeCodeExecutor((_, _, testCases) =>
                {
                    var totalTests = testCases.Count();
                    return new(
                        SubmissionStatus.Accepted,
                        "Accepted.",
                        totalTests,
                        totalTests,
                        40,
                        1024);
                }),
                feedbackQueue,
                NullLogger<SubmissionService>.Instance);

            var request = new CreateSubmissionRequestDto
            {
                ProblemId = problem.Id,
                InterviewSessionId = session.Id,
                Language = "csharp",
                SourceCode = "public class Solution { }"
            };

            var result = await service.CreateSubmissionAsync(candidate.Id, request);

            Assert.Equal(candidate.Id, result.CandidateId);
            Assert.Equal(problem.Id, result.ProblemId);
            Assert.Equal(session.Id, result.InterviewSessionId);
            Assert.Equal("Accepted", result.Status);
            Assert.Equal(SubmissionFeedbackStatuses.Pending, result.AiFeedbackStatus);
            Assert.Single(feedbackQueue.QueuedSubmissionIds, result.Id);
            Assert.Equal(1, db.Submissions.Count());
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldThrow_WhenProblemDoesNotBelongToInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var allowedProblem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 2, title: "Allowed");
            var otherProblem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 2, title: "Other");

            TestDataSeeder.AddProblemToInterview(db, interview.Id, allowedProblem.Id, points: 100, orderIndex: 1);

            var session = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.InProgress);

            var service = new SubmissionService(
                db,
                new FakeCodeExecutor(),
                new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            var request = new CreateSubmissionRequestDto
            {
                ProblemId = otherProblem.Id,
                InterviewSessionId = session.Id,
                Language = "csharp",
                SourceCode = "public class Solution { }"
            };

            var action = () => service.CreateSubmissionAsync(candidate.Id, request);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldThrow_WhenInterviewSessionIsNotActive()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 2);

            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, points: 100, orderIndex: 1);

            var session = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.Completed);

            var service = new SubmissionService(
                db,
                new FakeCodeExecutor(),
                new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            var request = new CreateSubmissionRequestDto
            {
                ProblemId = problem.Id,
                InterviewSessionId = session.Id,
                Language = "csharp",
                SourceCode = "public class Solution { }"
            };

            var action = () => service.CreateSubmissionAsync(candidate.Id, request);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        [Fact]
        public async Task GetMySubmissionsAsync_ShouldReturnOnlyCandidateSubmissions()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate1 = TestDataSeeder.CreateCandidate(db);
            var candidate2 = TestDataSeeder.CreateCandidate(db);

            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);

            TestDataSeeder.CreateSubmission(
                db,
                candidate1.Id,
                problem.Id,
                null,
                SubmissionStatus.Accepted,
                passedTests: 1,
                totalTests: 1);

            TestDataSeeder.CreateSubmission(
                db,
                candidate1.Id,
                problem.Id,
                null,
                SubmissionStatus.WrongAnswer,
                passedTests: 0,
                totalTests: 1);

            TestDataSeeder.CreateSubmission(
                db,
                candidate2.Id,
                problem.Id,
                null,
                SubmissionStatus.Accepted,
                passedTests: 1,
                totalTests: 1);

            var service = new SubmissionService(
                db,
                new FakeCodeExecutor(),
                new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            var result = (await service.GetMySubmissionsAsync(candidate1.Id)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, submission => Assert.Equal(candidate1.Id, submission.CandidateId));
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldPersistExecutorDiagnostics()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 2);
            var feedbackQueue = new FakeSubmissionFeedbackQueue();
            var service = new SubmissionService(
                db,
                new FakeCodeExecutor((_, _, __) => new(
                    SubmissionStatus.CompilationError,
                    "Missing Main method.",
                    0,
                    2,
                    null,
                    null)),
                feedbackQueue,
                NullLogger<SubmissionService>.Instance);

            var result = await service.CreateSubmissionAsync(candidate.Id, new CreateSubmissionRequestDto
            {
                ProblemId = problem.Id,
                Language = "csharp",
                SourceCode = "public class Solution { }"
            });

            Assert.Equal("CompilationError", result.Status);
            Assert.Equal(SubmissionFeedbackStatuses.Pending, result.AiFeedbackStatus);
            Assert.Single(feedbackQueue.QueuedSubmissionIds, result.Id);

            var storedSubmission = db.Submissions.Single(x => x.Id == result.Id);
            Assert.Equal("Missing Main method.", storedSubmission.ExecutionOutput);
            Assert.Equal(0, storedSubmission.PassedTests);
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldPersistSubmissionAndQueueAsyncFeedback()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var feedbackQueue = new FakeSubmissionFeedbackQueue();

            var service = new SubmissionService(
                db,
                new FakeCodeExecutor(),
                feedbackQueue,
                NullLogger<SubmissionService>.Instance);

            var result = await service.CreateSubmissionAsync(candidate.Id, new CreateSubmissionRequestDto
            {
                ProblemId = problem.Id,
                Language = "csharp",
                SourceCode = "public class Solution { }"
            });

            Assert.Null(result.AiFeedback);
            Assert.Null(result.AiFeedbackSource);
            Assert.Equal(SubmissionFeedbackStatuses.Pending, result.AiFeedbackStatus);
            Assert.Single(feedbackQueue.QueuedSubmissionIds, result.Id);

            var storedSubmission = db.Submissions.Single(x => x.Id == result.Id);
            Assert.Equal(SubmissionFeedbackStatuses.Pending, storedSubmission.AiFeedbackStatus);
        }

        [Fact]
        public async Task GetMySubmissionsAsync_ShouldClassifyFallbackFeedbackSource()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);

            TestDataSeeder.CreateSubmission(
                db,
                candidate.Id,
                problem.Id,
                null,
                SubmissionStatus.Accepted,
                passedTests: 1,
                totalTests: 1,
                aiFeedback: """
                    Overall
                    Nice work. This solution passed all available tests, which is a strong sign that your core approach is correct.

                    Correctness
                    You passed 1 out of 1 tests. That means the implementation handled the checked cases successfully.

                    Code Quality
                    Using a lookup-oriented data structure suggests good attention to runtime efficiency. The next polish point would be making the control flow easy to explain out loud.

                    Next Step
                    As a follow-up, explain the time and space complexity out loud and think about one edge case you would test next.
                    """);

            var service = new SubmissionService(
                db,
                new FakeCodeExecutor(),
                new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            var result = (await service.GetMySubmissionsAsync(candidate.Id)).Single();

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.AiFeedbackSource);
            Assert.Equal(SubmissionFeedbackStatuses.Ready, result.AiFeedbackStatus);
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldThrow_WhenProblemNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var candidate = TestDataSeeder.CreateCandidate(db);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateSubmissionAsync(candidate.Id, new CreateSubmissionRequestDto
                {
                    ProblemId = Guid.NewGuid(),
                    Language = "csharp",
                    SourceCode = "public class Solution { }"
                }));
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldThrow_WhenInterviewSessionBelongsToAnotherCandidate()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var owner = TestDataSeeder.CreateCandidate(db);
            var impersonator = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, owner.Id, InterviewSessionStatus.InProgress);

            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            // The session is owned by `owner`; `impersonator` must get 404, not 403,
            // so the session cannot be confirmed to exist.
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateSubmissionAsync(impersonator.Id, new CreateSubmissionRequestDto
                {
                    ProblemId = problem.Id,
                    InterviewSessionId = session.Id,
                    Language = "csharp",
                    SourceCode = "public class Solution { }"
                }));
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldPersistSubmission_WhenFeedbackQueueThrows()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);

            var throwingQueue = new ThrowingSubmissionFeedbackQueue();
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), throwingQueue,
                NullLogger<SubmissionService>.Instance);

            // Queue failure must not surface to the caller — submission execution
            // is the primary contract; AI feedback is best-effort.
            var result = await service.CreateSubmissionAsync(candidate.Id, new CreateSubmissionRequestDto
            {
                ProblemId = problem.Id,
                Language = "csharp",
                SourceCode = "public class Solution { }"
            });

            Assert.NotNull(result);
            Assert.Equal(SubmissionFeedbackStatuses.Pending, result.AiFeedbackStatus);
            Assert.Equal(1, db.Submissions.Count());
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldThrow_WhenInterviewSessionHasExpired()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id); // DurationMinutes = 60
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, points: 100, orderIndex: 1);

            var session = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.InProgress,
                startedAt: DateTime.UtcNow.AddMinutes(-61));

            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            var action = () => service.CreateSubmissionAsync(candidate.Id, new CreateSubmissionRequestDto
            {
                ProblemId = problem.Id,
                InterviewSessionId = session.Id,
                Language = "csharp",
                SourceCode = "public class Solution { }"
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(action);
            Assert.Equal("The interview session has expired.", ex.Message);
        }

        [Fact]
        public async Task CreateSubmissionAsync_ShouldSucceed_WhenInterviewSessionHasNotExpired()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id); // DurationMinutes = 60
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, points: 100, orderIndex: 1);

            var session = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.InProgress,
                startedAt: DateTime.UtcNow.AddMinutes(-30));

            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            var result = await service.CreateSubmissionAsync(candidate.Id, new CreateSubmissionRequestDto
            {
                ProblemId = problem.Id,
                InterviewSessionId = session.Id,
                Language = "csharp",
                SourceCode = "public class Solution { }"
            });

            Assert.Equal(candidate.Id, result.CandidateId);
            Assert.Equal(session.Id, result.InterviewSessionId);
        }

        private sealed class ThrowingSubmissionFeedbackQueue : ISubmissionFeedbackQueue
        {
            public ValueTask QueueAsync(Guid submissionId, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("Queue is unavailable.");
        }

        [Fact]
        public async Task ResetProblemAsync_ShouldDeleteOnlyMatchingPracticeSubmissions()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var otherCandidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var otherProblem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1, title: "Other");

            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, null, SubmissionStatus.Accepted);
            TestDataSeeder.CreateSubmission(db, candidate.Id, otherProblem.Id, null, SubmissionStatus.Accepted);
            TestDataSeeder.CreateSubmission(db, otherCandidate.Id, problem.Id, null, SubmissionStatus.Accepted);

            var service = new SubmissionService(
                db,
                new FakeCodeExecutor(),
                new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await service.ResetProblemAsync(candidate.Id, problem.Id, null);

            Assert.DoesNotContain(db.Submissions, s => s.CandidateId == candidate.Id && s.ProblemId == problem.Id && s.InterviewSessionId == null);
            Assert.Contains(db.Submissions, s => s.CandidateId == candidate.Id && s.ProblemId == otherProblem.Id);
            Assert.Contains(db.Submissions, s => s.CandidateId == otherCandidate.Id && s.ProblemId == problem.Id);
        }

        // ── GetByInterviewSessionAsync ────────────────────────────────────────

        [Fact]
        public async Task GetByInterviewSessionAsync_ShouldThrow_WhenSessionNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();
            var candidate = TestDataSeeder.CreateCandidate(db);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetByInterviewSessionAsync(Guid.NewGuid(), candidate.Id));
        }

        [Fact]
        public async Task GetByInterviewSessionAsync_ShouldReturnSessionSubmissions()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidate.Id, InterviewSessionStatus.InProgress);

            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, session.Id, SubmissionStatus.Accepted);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, session.Id, SubmissionStatus.WrongAnswer);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, null, SubmissionStatus.Accepted);

            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            var result = (await service.GetByInterviewSessionAsync(session.Id, candidate.Id)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Equal(session.Id, s.InterviewSessionId));
        }

        // ── ResetProblemAsync — session-mode branches ─────────────────────────

        [Fact]
        public async Task ResetProblemAsync_ShouldThrow_WhenInterviewSessionNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ResetProblemAsync(candidate.Id, problem.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task ResetProblemAsync_ShouldThrow_WhenInterviewSessionIsNotInProgress()
        {
            using var db = TestDbContextFactory.CreateContext();
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidate.Id, InterviewSessionStatus.Completed);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ResetProblemAsync(candidate.Id, problem.Id, session.Id));
        }

        [Fact]
        public async Task ResetProblemAsync_ShouldThrow_WhenProblemDoesNotBelongToInterview()
        {
            using var db = TestDbContextFactory.CreateContext();
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var inInterview = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1, title: "In");
            var notInInterview = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1, title: "Out");
            TestDataSeeder.AddProblemToInterview(db, interview.Id, inInterview.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidate.Id, InterviewSessionStatus.InProgress);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ResetProblemAsync(candidate.Id, notInInterview.Id, session.Id));
        }

        [Fact]
        public async Task ResetProblemAsync_ShouldDeleteSessionSubmissions_WhenInterviewSessionIdProvided()
        {
            using var db = TestDbContextFactory.CreateContext();
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidate.Id, InterviewSessionStatus.InProgress);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, session.Id, SubmissionStatus.Accepted);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, null, SubmissionStatus.Accepted);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await service.ResetProblemAsync(candidate.Id, problem.Id, session.Id);

            Assert.DoesNotContain(db.Submissions, s => s.InterviewSessionId == session.Id);
            Assert.Contains(db.Submissions, s => s.InterviewSessionId == null);
        }

        [Fact]
        public async Task ResetProblemAsync_ShouldReturnEarlyWithoutError_WhenNoSubmissionsExist()
        {
            using var db = TestDbContextFactory.CreateContext();
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await service.ResetProblemAsync(candidate.Id, problem.Id, null);

            Assert.Equal(0, db.Submissions.Count());
        }

        [Fact]
        public async Task ResetProblemAsync_ShouldThrow_WhenPracticeModeProblemNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();
            var candidate = TestDataSeeder.CreateCandidate(db);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ResetProblemAsync(candidate.Id, Guid.NewGuid(), null));
        }

        // ── ResetInterviewSessionAsync — error and edge paths ─────────────────

        [Fact]
        public async Task ResetInterviewSessionAsync_ShouldThrow_WhenSessionNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();
            var candidate = TestDataSeeder.CreateCandidate(db);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ResetInterviewSessionAsync(candidate.Id, Guid.NewGuid()));
        }

        [Fact]
        public async Task ResetInterviewSessionAsync_ShouldThrow_WhenSessionIsCompleted()
        {
            using var db = TestDbContextFactory.CreateContext();
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidate.Id, InterviewSessionStatus.Completed);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ResetInterviewSessionAsync(candidate.Id, session.Id));
        }

        [Fact]
        public async Task ResetInterviewSessionAsync_ShouldReturnEarlyWithoutError_WhenNoSubmissionsExist()
        {
            using var db = TestDbContextFactory.CreateContext();
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidate.Id, InterviewSessionStatus.InProgress);
            var service = new SubmissionService(
                db, new FakeCodeExecutor(), new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await service.ResetInterviewSessionAsync(candidate.Id, session.Id);

            Assert.Equal(0, db.Submissions.Count());
        }

        [Fact]
        public async Task ResetInterviewSessionAsync_ShouldDeleteOnlySessionSubmissions()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problemOne = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1, title: "One");
            var problemTwo = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 1, title: "Two");

            TestDataSeeder.AddProblemToInterview(db, interview.Id, problemOne.Id, points: 100, orderIndex: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problemTwo.Id, points: 100, orderIndex: 2);

            var session = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.InProgress);

            TestDataSeeder.CreateSubmission(db, candidate.Id, problemOne.Id, session.Id, SubmissionStatus.Accepted);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problemTwo.Id, session.Id, SubmissionStatus.WrongAnswer);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problemOne.Id, null, SubmissionStatus.Accepted);

            var service = new SubmissionService(
                db,
                new FakeCodeExecutor(),
                new FakeSubmissionFeedbackQueue(),
                NullLogger<SubmissionService>.Instance);

            await service.ResetInterviewSessionAsync(candidate.Id, session.Id);

            Assert.DoesNotContain(db.Submissions, s => s.CandidateId == candidate.Id && s.InterviewSessionId == session.Id);
            Assert.Contains(db.Submissions, s => s.CandidateId == candidate.Id && s.InterviewSessionId == null);
        }
    }
}
