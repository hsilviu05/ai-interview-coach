using AIInterviewCoach.Application.DTOs.Problems;
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
                CsharpStarterCode = definition.CsharpStarterCode.Trim(),
                PythonStarterCode = definition.PythonStarterCode.Trim(),
                CppStarterCode = definition.CppStarterCode.Trim(),
                CsharpHarnessTemplate = definition.CsharpHarnessTemplate.Trim(),
                PythonHarnessTemplate = definition.PythonHarnessTemplate.Trim(),
                CppHarnessTemplate = definition.CppHarnessTemplate.Trim(),
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
                CsharpStarterCode = definition.CsharpStarterCode,
                PythonStarterCode = definition.PythonStarterCode,
                CppStarterCode = definition.CppStarterCode,
                CsharpHarnessTemplate = definition.CsharpHarnessTemplate,
                PythonHarnessTemplate = definition.PythonHarnessTemplate,
                CppHarnessTemplate = definition.CppHarnessTemplate
            };
        }
    }
}
