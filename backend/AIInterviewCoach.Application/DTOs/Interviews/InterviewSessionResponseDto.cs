using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Application.DTOs.Interviews
{
    public class InterviewSessionResponseDto
    {
        public Guid Id { get; set; }
        public Guid InterviewId { get; set; }
        public Guid CandidateId { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public string Status { get; set; } = string.Empty;
        public int TotalScore { get; set; }
    }
}