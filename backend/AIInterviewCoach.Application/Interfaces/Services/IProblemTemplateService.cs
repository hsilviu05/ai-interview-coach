using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IProblemTemplateService
    {
        IReadOnlyList<ProblemTemplateResponseDto> GetTemplates();
    }
}
