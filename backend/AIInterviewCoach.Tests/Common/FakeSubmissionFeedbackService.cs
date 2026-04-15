using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Interfaces.Services;

namespace AIInterviewCoach.Tests.Common
{
    public sealed class FakeSubmissionFeedbackService : ISubmissionFeedbackService
    {
        private readonly Func<SubmissionFeedbackContextDto, SubmissionFeedbackResultDto> _generateFeedback;

        public FakeSubmissionFeedbackService(
            Func<SubmissionFeedbackContextDto, SubmissionFeedbackResultDto>? generateFeedback = null)
        {
            _generateFeedback = generateFeedback ?? (_ => new SubmissionFeedbackResultDto
            {
                Content = "Overall\nSolid direction.\n\nCorrectness\nGood progress.\n\nCode Quality\nReadable structure.\n\nNext Step\nKeep iterating.",
                Source = SubmissionFeedbackSources.OpenAI
            });
        }

        public Task<SubmissionFeedbackResultDto> GenerateFeedbackAsync(
            SubmissionFeedbackContextDto context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_generateFeedback(context));
        }
    }
}
