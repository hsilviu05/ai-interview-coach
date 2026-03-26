using AIInterviewCoach.Application.DTOs.Interviews;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IInterviewService
    {
        Task<InterviewResponseDto> CreateInterviewAsync(Guid interviewerId, CreateInterviewRequestDto request);
        Task<bool> AddProblemAsync(Guid interviewId, AddProblemToInterviewRequestDto request, Guid interviewerId);
        Task<InterviewResponseDto?> GetByIdAsync(Guid id);
        Task<InterviewResponseDto?> GetByTokenAsync(string token);
        Task<InterviewSessionResponseDto> StartSessionAsync(string token, Guid candidateId);
    }
}