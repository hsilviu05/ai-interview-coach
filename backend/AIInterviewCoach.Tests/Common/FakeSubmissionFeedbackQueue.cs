using AIInterviewCoach.Application.Interfaces.Services;

namespace AIInterviewCoach.Tests.Common
{
    public sealed class FakeSubmissionFeedbackQueue : ISubmissionFeedbackQueue
    {
        public List<Guid> QueuedSubmissionIds { get; } = [];

        public ValueTask QueueAsync(Guid submissionId, CancellationToken cancellationToken = default)
        {
            QueuedSubmissionIds.Add(submissionId);
            return ValueTask.CompletedTask;
        }
    }
}
