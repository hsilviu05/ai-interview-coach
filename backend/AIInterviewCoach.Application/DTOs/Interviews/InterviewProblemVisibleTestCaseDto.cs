namespace AIInterviewCoach.Application.DTOs.Interviews
{
    public class InterviewProblemVisibleTestCaseDto
    {
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }
}
