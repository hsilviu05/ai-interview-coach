using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIInterviewCoach.Tests.Services
{
    public class InterviewServiceTests
    {
        [Fact]
        public async Task StartSessionAsync_ShouldCreateNewSession_WhenNoneExists()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id, token: "start-token");

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.StartSessionAsync("start-token", candidate.Id);

            Assert.Equal(interview.Id, result.InterviewId);
            Assert.Equal(candidate.Id, result.CandidateId);
            Assert.Equal("InProgress", result.Status);
            Assert.Equal(1, db.InterviewSessions.Count());
        }

        [Fact]
        public async Task StartSessionAsync_ShouldReturnExistingSession_WhenInProgressSessionExists()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id, token: "existing-token");
            var existingSession = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.InProgress);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.StartSessionAsync("existing-token", candidate.Id);

            Assert.Equal(existingSession.Id, result.Id);
            Assert.Equal(1, db.InterviewSessions.Count());
        }

        [Fact]
        public async Task CompleteSessionAsync_ShouldMarkSessionCompleted_AndAwardPointsOnlyOncePerProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id, token: "complete-token");

            var problem1 = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 2, title: "Two Sum");
            var problem2 = TestDataSeeder.CreateProblem(db, interviewer.Id, testCaseCount: 2, title: "Valid Parentheses");

            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem1.Id, points: 100, orderIndex: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem2.Id, points: 50, orderIndex: 2);

            var session = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.InProgress);

            TestDataSeeder.CreateSubmission(
                db,
                candidate.Id,
                problem1.Id,
                session.Id,
                SubmissionStatus.Accepted,
                passedTests: 2,
                totalTests: 2);

            TestDataSeeder.CreateSubmission(
                db,
                candidate.Id,
                problem1.Id,
                session.Id,
                SubmissionStatus.Accepted,
                passedTests: 2,
                totalTests: 2);

            TestDataSeeder.CreateSubmission(
                db,
                candidate.Id,
                problem2.Id,
                session.Id,
                SubmissionStatus.WrongAnswer,
                passedTests: 1,
                totalTests: 2);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.CompleteSessionAsync(session.Id, candidate.Id);

            Assert.Equal("Completed", result.Status);
            Assert.Equal(100, result.TotalScore);
            Assert.NotNull(result.SubmittedAt);

            var updatedSession = db.InterviewSessions.Single(x => x.Id == session.Id);
            Assert.Equal(InterviewSessionStatus.Completed, updatedSession.Status);
            Assert.Equal(100, updatedSession.TotalScore);
            Assert.NotNull(updatedSession.SubmittedAt);
        }

        [Fact]
        public async Task CompleteSessionAsync_ShouldThrow_WhenSessionIsNotInProgress()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db,
                interview.Id,
                candidate.Id,
                InterviewSessionStatus.Abandoned);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var action = () => service.CompleteSessionAsync(session.Id, candidate.Id);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenInterviewerDoesNotOwnInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var otherInterviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, owner.Id);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetByIdAsync(interview.Id, otherInterviewer.Id, isAdmin: false));
        }

        [Fact]
        public async Task StartSessionAsync_ShouldThrow_WhenInterviewIsInactive()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            TestDataSeeder.CreateInterview(db, interviewer.Id, token: "inactive-token", isActive: false);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.StartSessionAsync("inactive-token", candidate.Id));
        }

        [Fact]
        public async Task StartSessionAsync_ShouldThrow_WhenTokenDoesNotExist()
        {
            using var db = TestDbContextFactory.CreateContext();

            var candidate = TestDataSeeder.CreateCandidate(db);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.StartSessionAsync("no-such-token", candidate.Id));
        }

        [Fact]
        public async Task CompleteSessionAsync_ShouldReturnExistingSession_WhenAlreadyCompleted()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidate.Id, InterviewSessionStatus.Completed);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            // Calling complete on an already-completed session is idempotent; it returns the
            // existing result rather than throwing, so the frontend can retry safely.
            var result = await service.CompleteSessionAsync(session.Id, candidate.Id);

            Assert.Equal("Completed", result.Status);
            Assert.Equal(session.Id, result.Id);
        }

        [Fact]
        public async Task CompleteSessionAsync_ShouldThrow_WhenSessionBelongsToAnotherCandidate()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidateA = TestDataSeeder.CreateCandidate(db);
            var candidateB = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(
                db, interview.Id, candidateA.Id, InterviewSessionStatus.InProgress);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            // The session query filters by candidateId; a different candidate gets 404, not 403.
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CompleteSessionAsync(session.Id, candidateB.Id));
        }

        [Fact]
        public async Task GetByTokenAsync_ShouldPopulateCentralizedStarterCode_WhenInterviewProblemUsesBlankStdinStarters()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id, token: "template-token");
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, title: "Template Check");
            problem.ExecutionMode = ProblemExecutionModes.Stdin;
            problem.CsharpStarterCode = string.Empty;
            problem.PythonStarterCode = string.Empty;
            problem.CppStarterCode = string.Empty;
            db.SaveChanges();

            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, orderIndex: 1);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.GetByTokenAsync("template-token");

            Assert.NotNull(result);
            Assert.Single(result!.Problems);
            Assert.Contains("using System;", result.Problems[0].CsharpStarterCode);
            Assert.Contains("import sys", result.Problems[0].PythonStarterCode);
            Assert.Contains("#include <algorithm>", result.Problems[0].CppStarterCode);
        }

        [Fact]
        public async Task GetByTokenAsync_ShouldUseFallbackText_WhenProblemFieldsAreEmpty()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id, token: "sparse-token");

            // Create a minimal problem — empty description, constraints, and example I/O
            // to exercise the fallback-text branches in the Build* helpers.
            var sparseProblem = new Problem
            {
                Id = Guid.NewGuid(),
                Title = "Sparse Problem",
                Description = string.Empty,
                ConstraintsText = string.Empty,
                ExampleInput = string.Empty,
                ExampleOutput = string.Empty,
                Difficulty = "Easy",
                Topic = "Arrays",
                IsPublic = true,
                CreatedByUserId = interviewer.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Problems.Add(sparseProblem);
            db.SaveChanges();
            TestDataSeeder.AddProblemToInterview(db, interview.Id, sparseProblem.Id, orderIndex: 1);

            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);
            var result = await service.GetByTokenAsync("sparse-token");

            Assert.NotNull(result);
            var p = Assert.Single(result!.Problems);
            Assert.Contains("No written description", p.Description);
            Assert.Contains("No additional constraints", p.ConstraintsText);
            Assert.Equal(string.Empty, p.ExampleInput);
            Assert.Equal(string.Empty, p.ExampleOutput);
        }

        // ── CreateInterviewAsync ──────────────────────────────────────────────

        [Fact]
        public async Task CreateInterviewAsync_ShouldPersistInterviewWithGeneratedToken()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.CreateInterviewAsync(interviewer.Id, new Application.DTOs.Interviews.CreateInterviewRequestDto
            {
                Title = "Backend Round",
                PositionName = "Software Engineer",
                Description = "60 min technical",
                DurationMinutes = 60
            });

            Assert.Equal(interviewer.Id, result.InterviewerId);
            Assert.Equal("Backend Round", result.Title);
            Assert.True(result.IsActive);
            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.Equal(1, db.Interviews.Count());
        }

        // ── AddProblemAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task AddProblemAsync_ShouldReturnFalse_WhenInterviewNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.AddProblemAsync(
                Guid.NewGuid(),
                new Application.DTOs.Interviews.AddProblemToInterviewRequestDto { ProblemId = problem.Id, OrderIndex = 1, Points = 100 },
                interviewer.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task AddProblemAsync_ShouldThrow_WhenInterviewerDoesNotOwnInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, owner.Id);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.AddProblemAsync(
                    interview.Id,
                    new Application.DTOs.Interviews.AddProblemToInterviewRequestDto { ProblemId = problem.Id, OrderIndex = 1, Points = 100 },
                    other.Id));
        }

        [Fact]
        public async Task AddProblemAsync_ShouldThrow_WhenProblemDoesNotExist()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.AddProblemAsync(
                    interview.Id,
                    new Application.DTOs.Interviews.AddProblemToInterviewRequestDto { ProblemId = Guid.NewGuid(), OrderIndex = 1, Points = 100 },
                    interviewer.Id));
        }

        [Fact]
        public async Task AddProblemAsync_ShouldThrow_WhenProblemAlreadyInInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddProblemAsync(
                    interview.Id,
                    new Application.DTOs.Interviews.AddProblemToInterviewRequestDto { ProblemId = problem.Id, OrderIndex = 1, Points = 100 },
                    interviewer.Id));
        }

        [Fact]
        public async Task AddProblemAsync_ShouldReturnTrue_AndPersistRelation_OnSuccess()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.AddProblemAsync(
                interview.Id,
                new Application.DTOs.Interviews.AddProblemToInterviewRequestDto { ProblemId = problem.Id, OrderIndex = 1, Points = 75 },
                interviewer.Id);

            Assert.True(result);
            var ip = db.InterviewProblems.Single();
            Assert.Equal(interview.Id, ip.InterviewId);
            Assert.Equal(problem.Id, ip.ProblemId);
            Assert.Equal(75, ip.Points);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenInterviewNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.GetByIdAsync(Guid.NewGuid(), interviewer.Id, isAdmin: false);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnInterview_WhenAdminAccessesAnyOwnersInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var admin = TestDataSeeder.CreateAdmin(db);
            var interview = TestDataSeeder.CreateInterview(db, owner.Id);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id, testCaseCount: 1);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, points: 50, orderIndex: 1);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            // Admin bypasses the `InterviewerId != interviewerId` ownership check.
            var result = await service.GetByIdAsync(interview.Id, admin.Id, isAdmin: true);

            Assert.NotNull(result);
            Assert.Equal(interview.Id, result!.Id);
            Assert.Single(result!.Problems);
        }

        // ── GetInterviewSessionsAsync ─────────────────────────────────────────

        [Fact]
        public async Task GetInterviewSessionsAsync_ShouldReturnSessions_ForInterviewOwner()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            TestDataSeeder.CreateInterviewSession(db, interview.Id, candidate.Id);
            TestDataSeeder.CreateInterviewSession(db, interview.Id, candidate.Id);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = (await service.GetInterviewSessionsAsync(interview.Id, interviewer.Id)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Equal(interview.Id, s.InterviewId));
        }

        [Fact]
        public async Task GetInterviewSessionsAsync_ShouldThrow_WhenInterviewNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetInterviewSessionsAsync(Guid.NewGuid(), interviewer.Id));
        }

        [Fact]
        public async Task GetInterviewSessionsAsync_ShouldThrow_WhenInterviewerDoesNotOwnInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, owner.Id);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetInterviewSessionsAsync(interview.Id, other.Id));
        }

        // ── GetInterviewSessionDetailsAsync ───────────────────────────────────

        [Fact]
        public async Task GetInterviewSessionDetailsAsync_ShouldReturnDetailsWithSubmissions_ForInterviewOwner()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(db, interview.Id, candidate.Id);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, session.Id,
                Domain.Enums.SubmissionStatus.Accepted, aiFeedbackStatus: Domain.Constants.SubmissionFeedbackStatuses.Ready, aiFeedback: "Great work.");
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, session.Id,
                Domain.Enums.SubmissionStatus.WrongAnswer, aiFeedbackStatus: Domain.Constants.SubmissionFeedbackStatuses.Pending);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = await service.GetInterviewSessionDetailsAsync(session.Id, interviewer.Id);

            Assert.Equal(session.Id, result.Session.Id);
            Assert.Equal(2, result.Submissions.Count);
            Assert.NotNull(result.Submissions.Single(s => s.AiFeedbackStatus == Domain.Constants.SubmissionFeedbackStatuses.Ready).AiFeedbackSource);
            Assert.Null(result.Submissions.Single(s => s.AiFeedbackStatus == Domain.Constants.SubmissionFeedbackStatuses.Pending).AiFeedbackSource);
        }

        [Fact]
        public async Task GetInterviewSessionDetailsAsync_ShouldThrow_WhenSessionNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetInterviewSessionDetailsAsync(Guid.NewGuid(), interviewer.Id));
        }

        [Fact]
        public async Task GetInterviewSessionDetailsAsync_ShouldThrow_WhenInterviewerDoesNotOwnSession()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var interview = TestDataSeeder.CreateInterview(db, owner.Id);
            var session = TestDataSeeder.CreateInterviewSession(db, interview.Id, candidate.Id);
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetInterviewSessionDetailsAsync(session.Id, other.Id));
        }

        // ── GetMineAsync ──────────────────────────────────────────────────────

        // ── GetByTokenAsync — blank-field fallback branches ──────────────────

        [Fact]
        public async Task GetByTokenAsync_ReturnsUnspecifiedDifficulty_WhenProblemDifficultyIsBlank()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id, token: "blank-difficulty-token");
            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Title = "Blank Difficulty",
                Description = "desc",
                Difficulty = string.Empty,
                Topic = "Arrays",
                IsPublic = true,
                CreatedByUserId = interviewer.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Problems.Add(problem);
            db.SaveChanges();
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, orderIndex: 1);

            var result = await new InterviewService(db, NullLogger<InterviewService>.Instance).GetByTokenAsync("blank-difficulty-token");

            Assert.NotNull(result);
            var p = Assert.Single(result!.Problems);
            Assert.Equal("Unspecified", p.Difficulty);
        }

        [Fact]
        public async Task GetByTokenAsync_ReturnsGeneralTopic_WhenProblemTopicIsBlank()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id, token: "blank-topic-token");
            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Title = "Blank Topic",
                Description = "desc",
                Difficulty = "Easy",
                Topic = string.Empty,
                IsPublic = true,
                CreatedByUserId = interviewer.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Problems.Add(problem);
            db.SaveChanges();
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, orderIndex: 1);

            var result = await new InterviewService(db, NullLogger<InterviewService>.Instance).GetByTokenAsync("blank-topic-token");

            Assert.NotNull(result);
            var p = Assert.Single(result!.Problems);
            Assert.Equal("General", p.Topic);
        }

        [Fact]
        public async Task GetByTokenAsync_UsesVisibleTestCaseInput_WhenExampleInputIsBlankButTestCasesExist()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id, token: "testcase-fallback-token");
            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Title = "TestCase Fallback",
                Description = "desc",
                Difficulty = "Easy",
                Topic = "Arrays",
                ExampleInput = string.Empty,
                ExampleOutput = string.Empty,
                IsPublic = true,
                CreatedByUserId = interviewer.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Problems.Add(problem);
            db.TestCases.Add(new TestCase
            {
                Id = Guid.NewGuid(),
                ProblemId = problem.Id,
                Input = "testcase-input",
                ExpectedOutput = "testcase-output",
                IsHidden = false,
                OrderIndex = 1
            });
            db.SaveChanges();
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id, orderIndex: 1);

            var result = await new InterviewService(db, NullLogger<InterviewService>.Instance).GetByTokenAsync("testcase-fallback-token");

            Assert.NotNull(result);
            var p = Assert.Single(result!.Problems);
            Assert.Equal("testcase-input", p.ExampleInput);
            Assert.Equal("testcase-output", p.ExampleOutput);
        }

        // ── GetMineAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetMineAsync_ShouldReturnOnlyOwnerInterviews()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            var interviewWithProblem = TestDataSeeder.CreateInterview(db, owner.Id, token: "owner-1");
            var problem = TestDataSeeder.CreateProblem(db, owner.Id, testCaseCount: 1);
            TestDataSeeder.AddProblemToInterview(db, interviewWithProblem.Id, problem.Id, orderIndex: 1);
            TestDataSeeder.CreateInterview(db, owner.Id, token: "owner-2");
            TestDataSeeder.CreateInterview(db, other.Id, token: "other-1");
            var service = new InterviewService(db, NullLogger<InterviewService>.Instance);

            var result = (await service.GetMineAsync(owner.Id)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, i => Assert.Equal(owner.Id, i.InterviewerId));
            Assert.Single(result.Single(i => i.AccessToken == "owner-1").Problems);
        }
    }
}
