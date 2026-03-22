using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IProblemService
    {
        Task<IEnumerable<ProblemResponseDto>> GetAllProblemsAsync();
        Task<ProblemResponseDto> GetProblemByIdAsync(Guid id);
        Task<ProblemResponseDto> CreateProblemAsync(Guid userId, CreateProblemRequestDto createRequest);
        Task<ProblemResponseDto?> UpdateProblemAsync(Guid id, Guid userId,UpdateProblemRequestDto updateRequest);
        Task<bool> DeleteProblemAsync(Guid id, Guid userId);
        Task<TestCaseResponseDto> AddTestCaseAsync(Guid problemId, CreateTestCaseRequestDto createTestCaseRequest);
        Task<IEnumerable<TestCaseResponseDto>> GetTestCasesAsync(Guid problemId, bool includeHidden = false);
    }
} 