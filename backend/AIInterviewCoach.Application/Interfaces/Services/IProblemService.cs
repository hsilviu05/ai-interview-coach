using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IProblemService
    {
        Task<IEnumerable<ProblemResponseDto>> GetAllProblemsAsync(Guid currentUserId, UserRole currentUserRole);
        Task<ProblemResponseDto> GetProblemByIdAsync(Guid id, Guid currentUserId, UserRole currentUserRole);
        Task<ProblemResponseDto> CreateProblemAsync(Guid userId, CreateProblemRequestDto createRequest);
        Task<ProblemResponseDto?> UpdateProblemAsync(Guid id, Guid userId, UpdateProblemRequestDto updateRequest);
        Task<bool> DeleteProblemAsync(Guid id, Guid userId);
        Task<TestCaseResponseDto> AddTestCaseAsync(
            Guid problemId,
            Guid currentUserId,
            bool isAdmin,
            CreateTestCaseRequestDto createTestCaseRequest);
        Task<IEnumerable<TestCaseResponseDto>> GetTestCasesAsync(
            Guid problemId,
            Guid currentUserId,
            bool isAdmin,
            bool includeHidden = false);
    }
}
