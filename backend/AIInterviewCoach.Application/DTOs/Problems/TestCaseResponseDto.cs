using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class TestCaseResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProblemId { get; set; }
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public bool IsHidden { get; set; } = false;
        public int OrderIndex { get; set; } = 0;
    }
}