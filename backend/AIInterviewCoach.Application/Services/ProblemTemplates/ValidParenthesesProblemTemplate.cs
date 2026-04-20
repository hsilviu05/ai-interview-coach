using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class ValidParenthesesProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "valid-parentheses",
                Name: "Valid Parentheses",
                Summary: "Exact string-to-bool signature with judge-ready Solution methods in all three languages.",
                Title: "Valid Parentheses",
                Description: "Determine whether the input string is valid by checking matching opening and closing brackets.",
                Difficulty: "Easy",
                Topic: "Stacks",
                ConstraintsText: "1 <= s.length <= 10^4\ns consists only of the characters '(', ')', '{', '}', '[' and ']'.",
                ExampleInput: "s = \"()[]{}\"",
                ExampleOutput: "true",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                Signature: new ProblemSignatureDefinitionDto
                {
                    InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                    CsharpMethodName = "IsValid",
                    PythonMethodName = "isValid",
                    CppMethodName = "isValid",
                    ReturnType = ProblemSignatureTypeKeys.Bool,
                    Parameters =
                    [
                        new ProblemSignatureParameterDto { Name = "s", Type = ProblemSignatureTypeKeys.String }
                    ]
                },
                CsharpStarterCode: string.Empty,
                PythonStarterCode: string.Empty,
                CppStarterCode: string.Empty,
                IncludeInStarterCatalog: true,
                TestCases:
                [
                    new ProblemTemplateTestCaseDefinition("{\"s\":\"()[]{}\"}", "true", false, 1),
                    new ProblemTemplateTestCaseDefinition("{\"s\":\"(]\"}", "false", true, 2),
                    new ProblemTemplateTestCaseDefinition("{\"s\":\"([{}])\"}", "true", true, 3)
                ]);
        }
    }
}
