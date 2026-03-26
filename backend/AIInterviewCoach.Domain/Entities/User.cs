using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole UserRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public CandidateStatistic? CandidateStatistics { get; set; }
        public ICollection<Problem> Problems { get; set; } = new List<Problem>();
    }
}