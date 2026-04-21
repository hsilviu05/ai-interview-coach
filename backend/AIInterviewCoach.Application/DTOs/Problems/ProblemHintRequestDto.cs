namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class ProblemHintRequestDto
    {
        public int Level { get; set; }
        public string Language { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
    }
}
