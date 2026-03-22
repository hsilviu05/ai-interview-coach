using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Domain.Entities
{
    public class CandidateStatistic
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public int ProblemsSolved { get; set; }
        public int TotalSubmissions { get; set; }
        public decimal AccuracyRate { get; set; }
        public int AverageExecutionTimeMs { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User? Candidate { get; set; }
    }
}