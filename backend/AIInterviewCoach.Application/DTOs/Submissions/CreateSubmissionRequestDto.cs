namespace AIInterviewCoach.Application.DTOs.Submissions
{
    public class CreateSubmissionRequestDto
    {
        public Guid ProblemId { get; set; }
        public Guid? InterviewSessionId { get; set; }
        public string Language { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
    }
}
