
namespace AIInterviewCoach.Application.DTOs.Interviews
{
    public class InterviewResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Guid InterviewerId { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<InterviewProblemDto> Problems { get; set; } = new();
    }
}