using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class GenericFunctionProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "generic-function",
                Name: "Generic Function Template",
                Summary: "All-purpose hidden-runner template that passes the full raw input string into a single method.",
                Title: "Generic Function Problem",
                Description: "Customize the starter signature and hidden runner for your specific problem.",
                Difficulty: "Easy",
                Topic: "General",
                ConstraintsText: "Generic starter template: the hidden runner passes the full raw input string into Solution.Solve(...). Replace the method name, parameters, return type, and parsing logic if you want a typed function-signature problem.",
                ExampleInput: "line 1 of input\nline 2 of input",
                ExampleOutput: "single expected output value",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public string Solve(string rawInput)
                    {
                        return rawInput.Trim();
                    }
                }
                """,
                PythonStarterCode:
                """
                class Solution:
                    def solve(self, rawInput: str) -> str:
                        return rawInput.strip()
                """,
                CppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    string solve(string rawInput) {
                        return rawInput;
                    }
                };
                """,
                Signature: new ProblemSignatureDefinitionDto
                {
                    InputBindingMode = ProblemSignatureInputBindingModes.RawText,
                    CsharpMethodName = "Solve",
                    PythonMethodName = "solve",
                    CppMethodName = "solve",
                    ReturnType = ProblemSignatureTypeKeys.String,
                    Parameters =
                    [
                        new ProblemSignatureParameterDto { Name = "rawInput", Type = ProblemSignatureTypeKeys.String }
                    ]
                },
                IncludeInStarterCatalog: false,
                TestCases: []);
        }
    }
}
