using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IPracticeProblemHintRequestService
    {
        Task<ProblemHintResponseDto> GeneratePracticeHintAsync(
            Guid problemId,
            Guid currentUserId,
            UserRole currentUserRole,
            ProblemHintRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
