namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class ProblemSummaryResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ConstraintsText { get; set; } = string.Empty;
        public string ExampleInput { get; set; } = string.Empty;
        public string ExampleOutput { get; set; } = string.Empty;
        public string ExecutionMode { get; set; } = string.Empty;
        public bool IsPublic { get; set; } = true;
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
