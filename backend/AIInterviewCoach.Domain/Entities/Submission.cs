using AIInterviewCoach.Domain.Constants;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Domain.Entities
{
    public class Submission
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public User Candidate { get; set; } = null!;
        public Guid ProblemId { get; set; }
        public Problem Problem { get; set; } = null!;
        public Guid? InterviewSessionId { get; set; }
        public InterviewSession? InterviewSession { get; set; }
        public string Language { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
        public int PassedTests { get; set; }
        public int TotalTests { get; set; }
        public int? ExecutionTimeMs { get; set; }
        public int? MemoryKb { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string? ExecutionOutput { get; set; }
        public string? AiFeedback { get; set; }
        public string AiFeedbackStatus { get; set; } = SubmissionFeedbackStatuses.Pending;
    }

}
