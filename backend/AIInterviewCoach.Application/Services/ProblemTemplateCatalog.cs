using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;
using AIInterviewCoach.Application.Services.ProblemTemplates;
using AIInterviewCoach.Domain.Entities;

namespace AIInterviewCoach.Application.Services
{
    public static class ProblemTemplateCatalog
    {
        public static IReadOnlyList<ProblemTemplateResponseDto> GetCreateProblemTemplates()
        {
            return ProblemTemplateRegistry.GetDefinitions()
                .Select(MapToResponseDto)
                .ToArray();
        }

        public static string ResolveVisibleStarterCode(
            string executionMode,
            string language,
            string? configuredStarterCode)
        {
            return StandaloneStarterCodeCatalog.ResolveVisibleStarterCode(
                executionMode,
                language,
                configuredStarterCode);
        }

        internal static IReadOnlyList<StarterProblemSeed> BuildStarterProblemSeeds(Guid createdByUserId)
        {
            return ProblemTemplateRegistry.GetDefinitions()
                .Where(definition => definition.IncludeInStarterCatalog)
                .Select(definition => BuildStarterProblemSeed(definition, createdByUserId))
                .ToArray();
        }

        private static StarterProblemSeed BuildStarterProblemSeed(
            ProblemTemplateDefinition definition,
            Guid createdByUserId)
        {
            var generatedArtifacts = definition.Signature is not null
                ? ProblemSignatureCodeGenerator.Generate(definition.Signature)
                : null;
            var problemId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var problem = new Problem
            {
                Id = problemId,
                Title = definition.Title,
                Description = definition.Description,
                Difficulty = definition.Difficulty,
                Topic = definition.Topic,
                ConstraintsText = definition.ConstraintsText,
                ExampleInput = definition.ExampleInput,
                ExampleOutput = definition.ExampleOutput,
                ExecutionMode = definition.ExecutionMode,
                SignatureDefinitionJson = definition.Signature is null
                    ? null
                    : ProblemSignatureSerializer.Serialize(definition.Signature),
                CsharpStarterCode = ResolveTemplateStarterCode(
                    definition.CsharpStarterCode,
                    generatedArtifacts?.CsharpStarterCode),
                PythonStarterCode = ResolveTemplateStarterCode(
                    definition.PythonStarterCode,
                    generatedArtifacts?.PythonStarterCode),
                CppStarterCode = ResolveTemplateStarterCode(
                    definition.CppStarterCode,
                    generatedArtifacts?.CppStarterCode),
                CsharpHarnessTemplate = generatedArtifacts?.CsharpHarnessCode ?? string.Empty,
                PythonHarnessTemplate = generatedArtifacts?.PythonHarnessCode ?? string.Empty,
                CppHarnessTemplate = generatedArtifacts?.CppHarnessCode ?? string.Empty,
                IsPublic = true,
                CreatedByUserId = createdByUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            var testCases = definition.TestCases
                .Select(testCase => new TestCase
                {
                    Id = Guid.NewGuid(),
                    ProblemId = problemId,
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    IsHidden = testCase.IsHidden,
                    OrderIndex = testCase.OrderIndex
                })
                .ToArray();

            return new StarterProblemSeed(problem, testCases);
        }

        private static ProblemTemplateResponseDto MapToResponseDto(ProblemTemplateDefinition definition)
        {
            var generatedArtifacts = definition.Signature is not null
                ? ProblemSignatureCodeGenerator.Generate(definition.Signature)
                : null;

            return new ProblemTemplateResponseDto
            {
                Key = definition.Key,
                Name = definition.Name,
                Summary = definition.Summary,
                Title = definition.Title,
                Description = definition.Description,
                Difficulty = definition.Difficulty,
                Topic = definition.Topic,
                ConstraintsText = definition.ConstraintsText,
                ExampleInput = definition.ExampleInput,
                ExampleOutput = definition.ExampleOutput,
                ExecutionMode = definition.ExecutionMode,
                Signature = definition.Signature,
                CsharpStarterCode = ResolveTemplateStarterCode(
                    definition.CsharpStarterCode,
                    generatedArtifacts?.CsharpStarterCode),
                PythonStarterCode = ResolveTemplateStarterCode(
                    definition.PythonStarterCode,
                    generatedArtifacts?.PythonStarterCode),
                CppStarterCode = ResolveTemplateStarterCode(
                    definition.CppStarterCode,
                    generatedArtifacts?.CppStarterCode)
            };
        }

        private static string ResolveTemplateStarterCode(
            string configuredStarterCode,
            string? generatedStarterCode)
        {
            return string.IsNullOrWhiteSpace(configuredStarterCode)
                ? generatedStarterCode?.Trim() ?? string.Empty
                : configuredStarterCode.Trim();
        }
    }
}
