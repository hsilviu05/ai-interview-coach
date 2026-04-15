using AIInterviewCoach.Application.DTOs.Submissions;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface ISubmissionFeedbackService
    {
        Task<SubmissionFeedbackResultDto> GenerateFeedbackAsync(
            SubmissionFeedbackContextDto context,
            CancellationToken cancellationToken = default);
    }
}
