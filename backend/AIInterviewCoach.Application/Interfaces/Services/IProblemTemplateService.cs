using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IProblemTemplateService
    {
        IReadOnlyList<ProblemTemplateResponseDto> GetTemplates();
        ProblemSignaturePreviewResponseDto GetSignaturePreview(ProblemSignatureDefinitionDto signature);
    }
}
