using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services;

namespace AIInterviewCoach.Tests.Services
{
    public class ProblemHintFallbackCatalogTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ProblemHintContextDto MakeContext(
            int level = 1,
            string title = "Unknown Problem",
            string topic = "Arrays") =>
            new()
            {
                Level = level,
                ProblemTitle = title,
                ProblemDescription = "Desc",
                Difficulty = "Easy",
                Topic = topic,
                ConstraintsText = "",
                ExampleInput = "",
                ExampleOutput = "",
                Language = "csharp",
                SourceCode = "return 0;",
            };

        // ── Known problem titles ──────────────────────────────────────────────

        [Theory]
        [InlineData("two sum",                         1, "earlier value")]
        [InlineData("two sum",                         2, "hash map")]
        [InlineData("two sum",                         3, "target - currentValue")]
        [InlineData("valid parentheses",               1, "most recent")]
        [InlineData("valid parentheses",               2, "stack")]
        [InlineData("valid parentheses",               3, "empty")]
        [InlineData("merge strings alternately",       1, "one character")]
        [InlineData("merge strings alternately",       2, "indices")]
        [InlineData("merge strings alternately",       3, "alternating")]
        [InlineData("best time to buy and sell stock", 1, "best buy")]
        [InlineData("best time to buy and sell stock", 2, "running minimum")]
        [InlineData("best time to buy and sell stock", 3, "currentPrice")]
        public void Generate_KnownProblem_ContainsExpectedFragment(
            string title, int level, string fragment)
        {
            var result = ProblemHintFallbackCatalog.Generate(MakeContext(level, title));

            Assert.Contains(fragment, result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Title normalisation ───────────────────────────────────────────────

        [Fact]
        public void Generate_NormalizesTitle_ToLowercase_BeforeDispatch()
        {
            var result = ProblemHintFallbackCatalog.Generate(MakeContext(title: "TWO SUM"));

            Assert.Contains("earlier value", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Generate_TrimsTitle_BeforeDispatch()
        {
            var result = ProblemHintFallbackCatalog.Generate(MakeContext(title: "  two sum  "));

            Assert.Contains("earlier value", result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Generic hint ──────────────────────────────────────────────────────

        [Theory]
        [InlineData(1, "Arrays", "arrays problems like this")]
        [InlineData(1, "",       "this problem")]
        [InlineData(2, "",       "name the exact piece")]
        [InlineData(3, "",       "by hand")]
        public void Generate_GenericHint_ContainsExpectedFragment(
            int level, string topic, string fragment)
        {
            var result = ProblemHintFallbackCatalog.Generate(
                MakeContext(level, "Unknown Problem", topic));

            Assert.Contains(fragment, result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Hint prefix format ────────────────────────────────────────────────

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Generate_AllHints_StartWithHintLevelPrefix(int level)
        {
            var result = ProblemHintFallbackCatalog.Generate(MakeContext(level, "two sum"));

            Assert.StartsWith($"Hint {level}", result);
        }
    }
}
