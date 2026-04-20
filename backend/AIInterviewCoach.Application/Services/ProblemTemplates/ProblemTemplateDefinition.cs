using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal sealed record ProblemTemplateDefinition(
        string Key,
        string Name,
        string Summary,
        string Title,
        string Description,
        string Difficulty,
        string Topic,
        string ConstraintsText,
        string ExampleInput,
        string ExampleOutput,
        string ExecutionMode,
        ProblemSignatureDefinitionDto? Signature,
        string CsharpStarterCode,
        string PythonStarterCode,
        string CppStarterCode,
        bool IncludeInStarterCatalog,
        IReadOnlyList<ProblemTemplateTestCaseDefinition> TestCases);

    internal sealed record ProblemTemplateTestCaseDefinition(
        string Input,
        string ExpectedOutput,
        bool IsHidden,
        int OrderIndex);
}
