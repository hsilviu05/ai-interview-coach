using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Constants;
using AIInterviewCoach.Domain.Enums;
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
                NullLogger<SubmissionFeedbackProcessor>.Instance);

            await processor.ProcessAsync(submission.Id);

            var storedSubmission = db.Submissions.Single(x => x.Id == submission.Id);
            Assert.Equal(SubmissionFeedbackStatuses.Failed, storedSubmission.AiFeedbackStatus);
            Assert.Null(storedSubmission.AiFeedback);
        }
    }
}
