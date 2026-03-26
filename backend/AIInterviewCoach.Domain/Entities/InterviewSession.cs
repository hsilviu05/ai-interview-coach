using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Domain.Entities
{
    public class InterviewSession
    {
        public Guid Id { get; set; }

        public Guid InterviewId { get; set; }
        public Interview Interview { get; set; } = null!;

        public Guid CandidateId { get; set; }
        public User Candidate { get; set; } = null!;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }

        public InterviewSessionStatus Status { get; set; } = InterviewSessionStatus.InProgress;

        public int TotalScore { get; set; } = 0;
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}