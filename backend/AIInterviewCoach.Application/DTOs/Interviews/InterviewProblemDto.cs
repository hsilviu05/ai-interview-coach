using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Application.DTOs.Interviews
{
    public class InterviewProblemDto
    {
        public Guid ProblemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ConstraintsText { get; set; } = string.Empty;
        public string ExampleInput { get; set; } = string.Empty;
        public string ExampleOutput { get; set; } = string.Empty;
        public string ExecutionMode { get; set; } = string.Empty;
        public string CsharpStarterCode { get; set; } = string.Empty;
        public string PythonStarterCode { get; set; } = string.Empty;
        public string CppStarterCode { get; set; } = string.Empty;
        public List<InterviewProblemVisibleTestCaseDto> VisibleTestCases { get; set; } = new();
        public int OrderIndex { get; set; }
        public int Points { get; set; }
    }
}
