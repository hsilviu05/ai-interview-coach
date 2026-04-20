using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class TwoSumProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "two-sum",
                Name: "Two Sum",
                Summary: "Exact array-and-target signature with judge-ready Solution methods in all three languages.",
                Title: "Two Sum",
                Description: "Return the indices of the two numbers such that they add up to the target.",
                Difficulty: "Easy",
                Topic: "Arrays",
                ConstraintsText: "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9\n-10^9 <= target <= 10^9\nExactly one valid answer exists.",
                ExampleInput: "nums = [2,7,11,15], target = 9",
                ExampleOutput: "[0,1]",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                Signature: new ProblemSignatureDefinitionDto
                {
                    InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                    CsharpMethodName = "TwoSum",
                    PythonMethodName = "twoSum",
                    CppMethodName = "twoSum",
                    ReturnType = ProblemSignatureTypeKeys.IntArray,
                    Parameters =
                    [
                        new ProblemSignatureParameterDto { Name = "nums", Type = ProblemSignatureTypeKeys.IntArray },
                        new ProblemSignatureParameterDto { Name = "target", Type = ProblemSignatureTypeKeys.Int }
                    ]
                },
                CsharpStarterCode: string.Empty,
                PythonStarterCode: string.Empty,
                CppStarterCode: string.Empty,
                IncludeInStarterCatalog: true,
                TestCases:
                [
                    new ProblemTemplateTestCaseDefinition("{\"nums\":[2,7,11,15],\"target\":9}", "[0,1]", false, 1),
                    new ProblemTemplateTestCaseDefinition("{\"nums\":[3,2,4],\"target\":6}", "[1,2]", true, 2),
                    new ProblemTemplateTestCaseDefinition("{\"nums\":[3,3],\"target\":6}", "[0,1]", true, 3)
                ]);
        }
    }
}
