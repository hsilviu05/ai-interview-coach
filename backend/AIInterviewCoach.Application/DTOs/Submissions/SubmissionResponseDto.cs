using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Application.DTOs.Submissions
{
    public class SubmissionResponseDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public Guid ProblemId { get; set; }
        public Guid? InterviewSessionId { get; set; }
        public string Language { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int PassedTests { get; set; }
        public int TotalTests { get; set; }
        public int? ExecutionTimeMs { get; set; }
        public int? MemoryKb { get; set; }
        public string? ExecutionOutput { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
