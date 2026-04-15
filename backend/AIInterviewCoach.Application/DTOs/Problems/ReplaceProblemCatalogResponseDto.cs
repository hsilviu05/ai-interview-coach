namespace AIInterviewCoach.Application.DTOs.Problems
{
    public class ReplaceProblemCatalogResponseDto
    {
        public int DeletedProblemCount { get; set; }
        public int DeletedInterviewCount { get; set; }
        public int DeletedSubmissionCount { get; set; }
        public int CreatedProblemCount { get; set; }
        public IReadOnlyList<string> CreatedProblemTitles { get; set; } = Array.Empty<string>();
    }
}
