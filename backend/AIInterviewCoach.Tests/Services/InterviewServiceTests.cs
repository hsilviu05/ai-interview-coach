using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Tests.Common;

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

            var service = new InterviewService(db);

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

            var service = new InterviewService(db);

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

            var service = new InterviewService(db);

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

            var service = new InterviewService(db);

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

            var service = new InterviewService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetByIdAsync(interview.Id, otherInterviewer.Id, isAdmin: false));
        }
    }
}
