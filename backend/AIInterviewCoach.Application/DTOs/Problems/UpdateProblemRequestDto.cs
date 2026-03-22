using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class UpdateProblemRequestDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Easy";
        public string Topic { get; set; } = string.Empty;
        public string ConstraintsText { get; set; } = string.Empty;
        public string ExampleInput { get; set; } = string.Empty;
        public string ExampleOutput { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
    }
}