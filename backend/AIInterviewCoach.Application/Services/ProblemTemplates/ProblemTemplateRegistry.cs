namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class ProblemTemplateRegistry
    {
        private static readonly IReadOnlyList<ProblemTemplateDefinition> Definitions =
        [
            GenericFunctionProblemTemplate.Create(),
            TwoSumProblemTemplate.Create(),
            ValidParenthesesProblemTemplate.Create(),
            MergeStringsAlternatelyProblemTemplate.Create(),
            BestTimeToBuyAndSellStockProblemTemplate.Create()
        ];

        public static IReadOnlyList<ProblemTemplateDefinition> GetDefinitions()
        {
            return Definitions;
        }
    }
}
