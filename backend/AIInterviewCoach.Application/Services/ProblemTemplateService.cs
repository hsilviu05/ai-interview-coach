using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Application.Services.ProblemSignatures;

namespace AIInterviewCoach.Application.Services
{
    public class ProblemTemplateService : IProblemTemplateService
    {
        public IReadOnlyList<ProblemTemplateResponseDto> GetTemplates()
        {
            return ProblemTemplateCatalog.GetCreateProblemTemplates();
        }

        public ProblemSignaturePreviewResponseDto GetSignaturePreview(ProblemSignatureDefinitionDto signature)
        {
            var generatedArtifacts = ProblemSignatureCodeGenerator.Generate(signature);

            return new ProblemSignaturePreviewResponseDto
            {
                CsharpStarterCode = generatedArtifacts.CsharpStarterCode,
                PythonStarterCode = generatedArtifacts.PythonStarterCode,
                CppStarterCode = generatedArtifacts.CppStarterCode
            };
        }
    }
}
