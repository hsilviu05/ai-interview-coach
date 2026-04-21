namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class ProblemHintContextDto
    {
        public int Level { get; set; }
        public string ProblemTitle { get; set; } = string.Empty;
        public string ProblemDescription { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ConstraintsText { get; set; } = string.Empty;
        public string ExampleInput { get; set; } = string.Empty;
        public string ExampleOutput { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
    }
}
