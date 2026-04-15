using AIInterviewCoach.Application.DTOs.Submissions;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface ISubmissionService
    {
        Task<SubmissionResponseDto> CreateSubmissionAsync(Guid candidateId, CreateSubmissionRequestDto request);
        Task<IEnumerable<SubmissionResponseDto>> GetMySubmissionsAsync(Guid candidateId);
        Task<IEnumerable<SubmissionResponseDto>> GetByInterviewSessionAsync(Guid interviewSessionId, Guid candidateId);
        Task ResetProblemAsync(Guid candidateId, Guid problemId, Guid? interviewSessionId);
        Task ResetInterviewSessionAsync(Guid candidateId, Guid interviewSessionId);
    }
}
