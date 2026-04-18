namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface ISubmissionFeedbackQueue
    {
        ValueTask QueueAsync(Guid submissionId, CancellationToken cancellationToken = default);
    }
}
