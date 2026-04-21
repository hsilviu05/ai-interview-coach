using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IProblemHintService
    {
        Task<ProblemHintResponseDto> GenerateHintAsync(
            ProblemHintContextDto context,
            CancellationToken cancellationToken = default);
    }
}
