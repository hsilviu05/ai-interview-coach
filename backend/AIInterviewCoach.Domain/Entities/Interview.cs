using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Domain.Entities
{
    public class Interview
    {
public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Guid InterviewerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InterviewProblem> InterviewProblems { get; set; } = new List<InterviewProblem>();
        public ICollection<InterviewSession> Sessions { get; set; } = new List<InterviewSession>();
    }
}