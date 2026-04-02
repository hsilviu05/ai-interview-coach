using AIInterviewCoach.Application.DTOs.Submissions;

namespace AIInterviewCoach.Application.DTOs.Interviews
{
    public class InterviewSessionDetailsDto
    {
        public InterviewSessionResponseDto Session { get; set; } = new();
        public List<SubmissionResponseDto> Submissions { get; set; } = new();
    }
}