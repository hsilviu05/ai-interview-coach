using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;

namespace AIInterviewCoach.Application.Services
{
    public class ProblemTemplateService : IProblemTemplateService
    {
        public IReadOnlyList<ProblemTemplateResponseDto> GetTemplates()
        {
            return ProblemTemplateCatalog.GetCreateProblemTemplates();
        }
    }
}
