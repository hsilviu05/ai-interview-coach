using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Services
{
    public static class ProblemHintFallbackCatalog
    {
        public static string Generate(ProblemHintContextDto context)
        {
            var normalizedTitle = context.ProblemTitle.Trim().ToLowerInvariant();

            return normalizedTitle switch
            {
                "two sum" => GenerateTwoSumHint(context.Level),
                "valid parentheses" => GenerateValidParenthesesHint(context.Level),
                "merge strings alternately" => GenerateMergeStringsHint(context.Level),
                "best time to buy and sell stock" => GenerateStockHint(context.Level),
                _ => GenerateGenericHint(context)
            };
        }

        private static string GenerateTwoSumHint(int level)
        {
            return level switch
            {
                1 => "Hint 1\nAs you scan the array, think about what earlier value would complete the current number to reach the target.",
                2 => "Hint 2\nA hash map from number to index lets you answer that complement question in constant time while staying in one pass.",
                _ => "Hint 3\nFor each value, check whether target - currentValue is already in the map before you store the current value. Return the stored index and the current index as soon as you find the match."
            };
        }

        private static string GenerateValidParenthesesHint(int level)
        {
            return level switch
            {
                1 => "Hint 1\nEach closing bracket must match the most recent unmatched opening bracket, not just any earlier one.",
                2 => "Hint 2\nA stack is the right structure here: push openings, then pop only when the next closing symbol matches the top.",
                _ => "Hint 3\nFail immediately if a closing bracket appears when the stack is empty or the top does not match. At the end, the stack must also be empty."
            };
        }

        private static string GenerateMergeStringsHint(int level)
        {
            return level switch
            {
                1 => "Hint 1\nThink about consuming one character from each word in turn until one word runs out.",
                2 => "Hint 2\nTwo indices plus a result builder keep the solution linear and easy to follow.",
                _ => "Hint 3\nAppend alternating characters while both indices are in range, then append the remaining suffix from the longer word."
            };
        }

        private static string GenerateStockHint(int level)
        {
            return level switch
            {
                1 => "Hint 1\nYou never need to look ahead more than one day at a time. What matters is the best buy price you have seen so far.",
                2 => "Hint 2\nTrack the running minimum price and compare each current price against it to compute the profit if you sold today.",
                _ => "Hint 3\nOn each step, update the best profit with currentPrice - minimumSoFar, then keep minimumSoFar equal to the lowest price seen up to that point. If prices only fall, the answer stays 0."
            };
        }

        private static string GenerateGenericHint(ProblemHintContextDto context)
        {
            var topicPrefix = string.IsNullOrWhiteSpace(context.Topic)
                ? "this problem"
                : $"{context.Topic.ToLowerInvariant()} problems like this";

            return context.Level switch
            {
                1 => $"Hint 1\nStart by identifying the core repeated question in the input. For {topicPrefix}, the right answer often comes from carrying a small amount of state as you scan.",
                2 => "Hint 2\nTry to name the exact piece of information that each step should preserve for the next one. That usually points you to the right data structure or running variable.",
                _ => "Hint 3\nWork through the example by hand and write down how your state changes after each element. Then make sure your implementation also handles the smallest edge case from the constraints."
            };
        }
    }
}
