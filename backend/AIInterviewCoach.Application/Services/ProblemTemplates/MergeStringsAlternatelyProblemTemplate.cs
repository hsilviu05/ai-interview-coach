using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class MergeStringsAlternatelyProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "merge-strings-alternately",
                Name: "Merge Strings Alternately",
                Summary: "Exact two-string merge signature with judge-ready Solution methods in all three languages.",
                Title: "Merge Strings Alternately",
                Description: "Merge two strings by adding letters in alternating order, starting with the first string.",
                Difficulty: "Easy",
                Topic: "Strings",
                ConstraintsText: "1 <= word1.length, word2.length <= 100\nword1 and word2 consist of lowercase English letters.",
                ExampleInput: "word1 = \"abc\", word2 = \"pqr\"",
                ExampleOutput: "apbqcr",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                Signature: new ProblemSignatureDefinitionDto
                {
                    InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                    CsharpMethodName = "MergeAlternately",
                    PythonMethodName = "mergeAlternately",
                    CppMethodName = "mergeAlternately",
                    ReturnType = ProblemSignatureTypeKeys.String,
                    Parameters =
                    [
                        new ProblemSignatureParameterDto { Name = "word1", Type = ProblemSignatureTypeKeys.String },
                        new ProblemSignatureParameterDto { Name = "word2", Type = ProblemSignatureTypeKeys.String }
                    ]
                },
                CsharpStarterCode: string.Empty,
                PythonStarterCode: string.Empty,
                CppStarterCode: string.Empty,
                IncludeInStarterCatalog: true,
                TestCases:
                [
                    new ProblemTemplateTestCaseDefinition("{\"word1\":\"abc\",\"word2\":\"pqr\"}", "apbqcr", false, 1),
                    new ProblemTemplateTestCaseDefinition("{\"word1\":\"ab\",\"word2\":\"pqrs\"}", "apbqrs", true, 2),
                    new ProblemTemplateTestCaseDefinition("{\"word1\":\"abcd\",\"word2\":\"pq\"}", "apbqcd", true, 3)
                ]);
        }
    }
}
