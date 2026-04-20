using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Domain.Entities
{
    public class Problem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ConstraintsText { get; set; } = string.Empty;
        public string ExampleInput { get; set; } = string.Empty;
        public string ExampleOutput { get; set; } = string.Empty;
        public string ExecutionMode { get; set; } = ProblemExecutionModes.Stdin;
        public string? SignatureDefinitionJson { get; set; }
        public string CsharpStarterCode { get; set; } = string.Empty;
        public string PythonStarterCode { get; set; } = string.Empty;
        public string CppStarterCode { get; set; } = string.Empty;
        public string CsharpHarnessTemplate { get; set; } = string.Empty;
        public string PythonHarnessTemplate { get; set; } = string.Empty;
        public string CppHarnessTemplate { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        public Guid CreatedByUserId { get; set; } = Guid.Empty;
        public User CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
    }
}
