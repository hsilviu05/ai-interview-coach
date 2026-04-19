using System.Diagnostics;
using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIInterviewCoach.Application.Services
{
    public class SubmissionFeedbackProcessor
    {
        private readonly IAppDbContext _dbContext;
        private readonly ISubmissionFeedbackService _submissionFeedbackService;
        private readonly IObservabilityService _observabilityService;
        private readonly ILogger<SubmissionFeedbackProcessor> _logger;

        public SubmissionFeedbackProcessor(
            IAppDbContext dbContext,
            ISubmissionFeedbackService submissionFeedbackService,
            IObservabilityService observabilityService,
            ILogger<SubmissionFeedbackProcessor> logger)
        {
            _dbContext = dbContext;
            _submissionFeedbackService = submissionFeedbackService;
            _observabilityService = observabilityService;
            _logger = logger;
        }

        public async Task ProcessAsync(Guid submissionId, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var submission = await _dbContext.Submissions
                .Include(s => s.Problem)
                .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

            if (submission is null)
            {
                _logger.LogDebug(
                    "Skipping AI feedback generation because submission {SubmissionId} no longer exists.",
                    submissionId);
                return;
            }

            if (!string.Equals(
                    submission.AiFeedbackStatus,
                    SubmissionFeedbackStatuses.Pending,
                    StringComparison.Ordinal))
            {
                return;
            }

            if (submission.Problem is null)
            {
                submission.AiFeedbackStatus = SubmissionFeedbackStatuses.Failed;
                await _dbContext.SaveChangesAsync(cancellationToken);
                stopwatch.Stop();
                _observabilityService.RecordAiFeedback(
                    SubmissionFeedbackStatuses.Failed,
                    "MissingProblem",
                    stopwatch.Elapsed);

                _logger.LogWarning(
                    "Skipping AI feedback generation because submission {SubmissionId} is missing its problem.",
                    submissionId);
                return;
            }

            string? feedbackSource = null;

            try
            {
                var feedbackResult = await _submissionFeedbackService.GenerateFeedbackAsync(
                    BuildFeedbackContext(submission),
                    cancellationToken);

                submission.AiFeedback = feedbackResult.Content.Trim();
                submission.AiFeedbackStatus = SubmissionFeedbackStatuses.Ready;
                feedbackSource = feedbackResult.Source;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected AI feedback failure for submission {SubmissionId}.",
                    submissionId);

                submission.AiFeedback = null;
                submission.AiFeedbackStatus = SubmissionFeedbackStatuses.Failed;
                feedbackSource = "UnhandledException";
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();
            _observabilityService.RecordAiFeedback(
                submission.AiFeedbackStatus,
                feedbackSource,
                stopwatch.Elapsed);
        }

        private static SubmissionFeedbackContextDto BuildFeedbackContext(Domain.Entities.Submission submission)
        {
            var problem = submission.Problem;

            return new SubmissionFeedbackContextDto
            {
                ProblemTitle = problem.Title,
                ProblemDescription = problem.Description,
                Difficulty = problem.Difficulty,
                Topic = problem.Topic,
                ConstraintsText = problem.ConstraintsText,
                ExampleInput = problem.ExampleInput,
                ExampleOutput = problem.ExampleOutput,
                Language = submission.Language,
                SourceCode = submission.SourceCode,
                Status = submission.Status.ToString(),
                PassedTests = submission.PassedTests,
                TotalTests = submission.TotalTests,
                ExecutionOutput = submission.ExecutionOutput,
                IsPracticeMode = !submission.InterviewSessionId.HasValue
            };
        }
    }
}
