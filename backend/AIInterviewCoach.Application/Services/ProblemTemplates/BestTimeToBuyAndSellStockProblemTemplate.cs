using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class BestTimeToBuyAndSellStockProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "best-time-to-buy-sell-stock",
                Name: "Best Time to Buy and Sell Stock",
                Summary: "Exact array-profit signature with judge-ready Solution methods in all three languages.",
                Title: "Best Time to Buy and Sell Stock",
                Description: "Find the maximum profit from a single buy and a single sell of the stock.",
                Difficulty: "Easy",
                Topic: "Dynamic Programming",
                ConstraintsText: "1 <= prices.length <= 10^5\n0 <= prices[i] <= 10^4",
                ExampleInput: "prices = [7,1,5,3,6,4]",
                ExampleOutput: "5",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                Signature: new ProblemSignatureDefinitionDto
                {
                    InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                    CsharpMethodName = "MaxProfit",
                    PythonMethodName = "maxProfit",
                    CppMethodName = "maxProfit",
                    ReturnType = ProblemSignatureTypeKeys.Int,
                    Parameters =
                    [
                        new ProblemSignatureParameterDto { Name = "prices", Type = ProblemSignatureTypeKeys.IntArray }
                    ]
                },
                CsharpStarterCode: string.Empty,
                PythonStarterCode: string.Empty,
                CppStarterCode: string.Empty,
                IncludeInStarterCatalog: true,
                TestCases:
                [
                    new ProblemTemplateTestCaseDefinition("{\"prices\":[7,1,5,3,6,4]}", "5", false, 1),
                    new ProblemTemplateTestCaseDefinition("{\"prices\":[7,6,4,3,1]}", "0", true, 2),
                    new ProblemTemplateTestCaseDefinition("{\"prices\":[2,4,1]}", "2", true, 3)
                ]);
        }
    }
}
