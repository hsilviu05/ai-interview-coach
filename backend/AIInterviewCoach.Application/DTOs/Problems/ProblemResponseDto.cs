using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class ProblemResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ConstraintsText { get; set; } = string.Empty;
        public string ExampleInput { get; set; } = string.Empty;
        public string ExampleOutput { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}