namespace AIInterviewCoach.Application.DTOs.Submissions
{
    public class SubmissionFeedbackContextDto
    {
        public string ProblemTitle { get; set; } = string.Empty;
        public string ProblemDescription { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ConstraintsText { get; set; } = string.Empty;
        public string ExampleInput { get; set; } = string.Empty;
        public string ExampleOutput { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int PassedTests { get; set; }
        public int TotalTests { get; set; }
        public string? ExecutionOutput { get; set; }
        public bool IsPracticeMode { get; set; }
    }
}
