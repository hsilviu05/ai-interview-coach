using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Tests.Common;

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

            var service = new SubmissionService(db);

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
            Assert.Equal("WrongAnswer", result.Status);
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

            var service = new SubmissionService(db);

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

            var service = new SubmissionService(db);

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

            var service = new SubmissionService(db);

            var result = (await service.GetMySubmissionsAsync(candidate1.Id)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, submission => Assert.Equal(candidate1.Id, submission.CandidateId));
        }
    }
}