using AIInterviewCoach.Application.DTOs.Interviews;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IInterviewService
    {
        Task<InterviewResponseDto> CreateInterviewAsync(Guid interviewerId, CreateInterviewRequestDto request);
        Task<bool> AddProblemAsync(Guid interviewId, AddProblemToInterviewRequestDto request, Guid interviewerId);
        Task<InterviewResponseDto?> GetByIdAsync(Guid id, Guid interviewerId, bool isAdmin);
        Task<InterviewResponseDto?> GetByTokenAsync(string token);
        Task<InterviewSessionResponseDto> StartSessionAsync(string token, Guid candidateId);
        Task<InterviewSessionResponseDto> CompleteSessionAsync(Guid sessionId, Guid candidateId);
        Task<IEnumerable<InterviewSessionResponseDto>> GetInterviewSessionsAsync(Guid interviewId, Guid interviewerId);
        Task<InterviewSessionDetailsDto> GetInterviewSessionDetailsAsync(Guid sessionId, Guid interviewerId);
        Task<IEnumerable<InterviewResponseDto>> GetMineAsync(Guid interviewerId);
    }
}
