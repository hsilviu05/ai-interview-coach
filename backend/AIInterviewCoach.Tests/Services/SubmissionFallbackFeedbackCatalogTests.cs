using AIInterviewCoach.Application.DTOs.Submissions;

namespace AIInterviewCoach.Tests.Services
{
    public class SubmissionFallbackFeedbackCatalogTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static SubmissionFeedbackContextDto MakeContext(
            string status = "Accepted",
            bool isPracticeMode = false,
            int passedTests = 3,
            int totalTests = 3,
            string? executionOutput = null,
            string sourceCode = "return 42;") =>
            new()
            {
                ProblemTitle = "Test",
                ProblemDescription = "Desc",
                Difficulty = "Easy",
                Topic = "Arrays",
                ConstraintsText = "",
                ExampleInput = "",
                ExampleOutput = "",
                Language = "csharp",
                SourceCode = sourceCode,
                Status = status,
                PassedTests = passedTests,
                TotalTests = totalTests,
                ExecutionOutput = executionOutput,
                IsPracticeMode = isPracticeMode,
            };

        // ── Overall section ───────────────────────────────────────────────────

        [Theory]
        [InlineData("Accepted",          false, "interview-ready")]
        [InlineData("Accepted",          true,  "strong sign")]
        [InlineData("CompilationError",  false, "compilation")]
        [InlineData("RuntimeError",      false, "breaks at runtime")]
        [InlineData("TimeLimitExceeded", false, "algorithm efficiency")]
        [InlineData("WrongAnswer",       false, "wrong output")]
        [InlineData("Unknown",           false, "still a gap")]
        public void Generate_OverallSection_ContainsExpectedFragment(
            string status, bool isPractice, string fragment)
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(status, isPracticeMode: isPractice));

            Assert.Contains(fragment, result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Correctness section ───────────────────────────────────────────────

        [Fact]
        public void Generate_CorrectnessSection_ReturnsNoTestSummary_WhenTotalTestsIsZero()
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(totalTests: 0, passedTests: 0));

            Assert.Contains("No test summary", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Generate_CorrectnessSection_IncludesPassedCount_WhenAccepted()
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(status: "Accepted", passedTests: 2, totalTests: 5));

            Assert.Contains("2 out of 5", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Generate_CorrectnessSection_ReturnsNoDiagnostics_WhenExecutionOutputIsEmpty()
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(status: "WrongAnswer", executionOutput: null));

            Assert.Contains("did not return extra diagnostics", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Generate_CorrectnessSection_IncludesDiagnostics_WhenExecutionOutputIsPresent()
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(status: "RuntimeError", executionOutput: "Index out of range"));

            Assert.Contains("Runner diagnostics:", result, StringComparison.Ordinal);
            Assert.Contains("Index out of range", result, StringComparison.Ordinal);
        }

        // ── Code quality section ──────────────────────────────────────────────

        [Fact]
        public void Generate_CodeQualitySection_FlagsTodo_WhenPresentInSourceCode()
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(sourceCode: "// TODO: implement this\nreturn 0;"));

            Assert.Contains("TODO marker", result, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("for (int i=0;i<n;i++) { for (int j=0;j<n;j++) { } }")]      // two for loops
        [InlineData("foreach(var x in xs){ } foreach(var y in ys){ }")]          // two foreach loops
        [InlineData("for(int i=0;i<n;i++){ } foreach(var x in xs){ }")]          // for + foreach
        public void Generate_CodeQualitySection_FlagsNestedLoop_WhenMultipleLoopsDetected(
            string sourceCode)
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(sourceCode: sourceCode));

            Assert.Contains("repeated scans", result, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("var map = new Dictionary<int,int>(); return map[0];")]
        [InlineData("var seen = new HashSet<int>(); seen.Add(1);")]
        public void Generate_CodeQualitySection_PraisesLookupStructure_WhenDetected(
            string sourceCode)
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(sourceCode: sourceCode));

            Assert.Contains("lookup-oriented", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Generate_CodeQualitySection_PraisesLookupStructure_EvenWhenNestedLoopAlsoPresent()
        {
            // LookupStructure wins over NestedLoop in priority order:
            // `likelyNestedLoop && !usesLookupStructure` is false when usesLookupStructure is true.
            const string code =
                "var dict = new Dictionary<int,int>();" +
                "for(int i=0;i<n;i++) { for(int j=0;j<n;j++) { dict[i]=j; } }";

            var result = SubmissionFallbackFeedbackCatalog.Generate(MakeContext(sourceCode: code));

            Assert.Contains("lookup-oriented", result, StringComparison.Ordinal);
            Assert.DoesNotContain("repeated scans", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Generate_CodeQualitySection_FlagsLongMethod_WhenOver35Lines()
        {
            var longCode = string.Join("\n", Enumerable.Range(0, 36).Select(i => $"var x{i} = {i};"));

            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(sourceCode: longCode));

            Assert.Contains("one larger block", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Generate_CodeQualitySection_ReturnsDefaultMessage_ForCompactCleanCode()
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(sourceCode: "return nums[0] + nums[1];"));

            Assert.Contains("fairly compact", result, StringComparison.Ordinal);
        }

        // ── Next Step section ─────────────────────────────────────────────────

        [Theory]
        [InlineData("Accepted",          false, "tradeoffs")]
        [InlineData("Accepted",          true,  "complexity out loud")]
        [InlineData("CompilationError",  false, "compiler errors")]
        [InlineData("RuntimeError",      false, "hand-worked example")]
        [InlineData("TimeLimitExceeded", false, "precomputation")]
        [InlineData("WrongAnswer",       false, "boundary conditions")]
        [InlineData("Unknown",           false, "one minimal sample")]
        public void Generate_NextStepSection_ContainsExpectedFragment(
            string status, bool isPractice, string fragment)
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(
                MakeContext(status, isPracticeMode: isPractice));

            Assert.Contains(fragment, result, StringComparison.OrdinalIgnoreCase);
        }

        // ── Output shape ──────────────────────────────────────────────────────

        [Fact]
        public void Generate_OutputContainsAllFourSectionHeaders()
        {
            var result = SubmissionFallbackFeedbackCatalog.Generate(MakeContext());

            Assert.Contains("Overall\n", result, StringComparison.Ordinal);
            Assert.Contains("\n\nCorrectness\n", result, StringComparison.Ordinal);
            Assert.Contains("\n\nCode Quality\n", result, StringComparison.Ordinal);
            Assert.Contains("\n\nNext Step\n", result, StringComparison.Ordinal);
        }
    }

    // ── SubmissionFeedbackSourceResolver ─────────────────────────────────────

    public class SubmissionFeedbackSourceResolverTests
    {
        // ── ResolveSource ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ResolveSource_ReturnsNull_ForNullOrWhitespace(string? input)
        {
            Assert.Null(SubmissionFeedbackSourceResolver.ResolveSource(input));
        }

        [Fact]
        public void ResolveSource_ReturnsLocalFallback_WhenFeedbackMatchesCatalogShape()
        {
            var generated = SubmissionFallbackFeedbackCatalog.Generate(new SubmissionFeedbackContextDto
            {
                ProblemTitle = "X", ProblemDescription = "", Difficulty = "Easy",
                Topic = "Arrays", ConstraintsText = "", ExampleInput = "", ExampleOutput = "",
                Language = "csharp", SourceCode = "return 0;",
                Status = "Accepted", PassedTests = 1, TotalTests = 1,
                IsPracticeMode = false,
            });

            Assert.Equal(
                SubmissionFeedbackSources.LocalFallback,
                SubmissionFeedbackSourceResolver.ResolveSource(generated));
        }

        [Fact]
        public void ResolveSource_ReturnsOpenAI_ForArbitraryNonFallbackContent()
        {
            const string aiContent = "This is a custom AI response with no known fallback fragments.";

            Assert.Equal(
                SubmissionFeedbackSources.OpenAI,
                SubmissionFeedbackSourceResolver.ResolveSource(aiContent));
        }

        // ── LooksLikeLocalFallback ────────────────────────────────────────────

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void LooksLikeLocalFallback_ReturnsFalse_ForWhitespaceInput(string input)
        {
            // Exercises the IsNullOrWhiteSpace guard inside LooksLikeLocalFallback
            // (unreachable via ResolveSource which already gates on whitespace).
            Assert.False(SubmissionFeedbackSourceResolver.LooksLikeLocalFallback(input));
        }

        [Fact]
        public void LooksLikeLocalFallback_ReturnsFalse_WhenShapeMatchesButNoKnownFragmentPresent()
        {
            // Correct section structure but all body text is novel → no known fragment matches.
            const string customFeedback =
                "Overall\n" +
                "Completely custom overall text with no known fragments.\n\n" +
                "Correctness\n" +
                "Custom correctness message.\n\n" +
                "Code Quality\n" +
                "Custom code quality message.\n\n" +
                "Next Step\n" +
                "Custom next step message.";

            Assert.False(SubmissionFeedbackSourceResolver.LooksLikeLocalFallback(customFeedback));
        }
    }
}
