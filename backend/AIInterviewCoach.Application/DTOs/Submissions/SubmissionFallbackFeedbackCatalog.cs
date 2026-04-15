using System.Text;

namespace AIInterviewCoach.Application.DTOs.Submissions
{
    public static class SubmissionFallbackFeedbackCatalog
    {
        public static IReadOnlyList<string> KnownFragments { get; } = new[]
        {
            AcceptedPracticeOverall,
            AcceptedInterviewOverall,
            CompilationErrorOverall,
            RuntimeErrorOverall,
            TimeLimitExceededOverall,
            WrongAnswerOverall,
            DefaultOverall,
            NoTestSummaryCorrectness,
            AcceptedCorrectnessSuffix,
            NoDiagnosticsCorrectness,
            TodoCodeQuality,
            NestedLoopCodeQuality,
            LookupStructureCodeQuality,
            LongMethodCodeQuality,
            DefaultCodeQuality,
            AcceptedPracticeNextStep,
            AcceptedInterviewNextStep,
            CompilationErrorNextStep,
            RuntimeErrorNextStep,
            TimeLimitExceededNextStep,
            WrongAnswerNextStep,
            DefaultNextStep
        };

        private const string AcceptedPracticeOverall =
            "Nice work. This solution passed all available tests, which is a strong sign that your core approach is correct.";
        private const string AcceptedInterviewOverall =
            "This submission cleared the current test suite, so your main approach is interview-ready from a correctness standpoint.";
        private const string CompilationErrorOverall =
            "The main blocker is compilation. The algorithm may be on the right track, but the code needs to build cleanly before it can be evaluated.";
        private const string RuntimeErrorOverall =
            "Your logic gets far enough to execute, but the submission still breaks at runtime. That usually points to an unchecked edge case or unsafe assumption.";
        private const string TimeLimitExceededOverall =
            "The implementation is likely doing more work than the input limits comfortably allow. The next improvement should focus on algorithm efficiency.";
        private const string WrongAnswerOverall =
            "The structure of the solution is in place, but at least one branch of the logic is producing the wrong output.";
        private const string DefaultOverall =
            "This submission shows progress, but there is still a gap between the current implementation and a fully reliable solution.";

        private const string NoTestSummaryCorrectness =
            "No test summary was available, so use the execution output and the sample case to verify the behavior manually.";
        private const string AcceptedCorrectnessSuffix =
            "That means the implementation handled the checked cases successfully.";
        private const string NoDiagnosticsCorrectness =
            "The runner did not return extra diagnostics.";

        private const string TodoCodeQuality =
            "There is still a TODO marker in the submission. Before finalizing, remove placeholder logic and make sure each branch reflects the intended algorithm.";
        private const string NestedLoopCodeQuality =
            "The code is readable, but it may be doing repeated scans over the input. Double-check whether the constraints would benefit from a lookup structure or a single-pass approach.";
        private const string LookupStructureCodeQuality =
            "Using a lookup-oriented data structure suggests good attention to runtime efficiency. The next polish point would be making the control flow easy to explain out loud.";
        private const string LongMethodCodeQuality =
            "The implementation works through the problem in one larger block. Extracting one or two helper methods would make it easier to reason about and explain in an interview.";
        private const string DefaultCodeQuality =
            "The submission is fairly compact and easy to scan. Keep aiming for descriptive variable names and a control flow you can justify step by step.";

        private const string AcceptedPracticeNextStep =
            "As a follow-up, explain the time and space complexity out loud and think about one edge case you would test next.";
        private const string AcceptedInterviewNextStep =
            "Be ready to explain the tradeoffs, the big-O complexity, and why this approach is safer than the most obvious brute-force version.";
        private const string CompilationErrorNextStep =
            "Fix compiler errors first, then rerun the simplest sample input before touching the algorithm again.";
        private const string RuntimeErrorNextStep =
            "Trace the code with a tiny hand-worked example and inspect every place where input shape, indexing, or parsing assumptions could break.";
        private const string TimeLimitExceededNextStep =
            "Look for the most expensive repeated operation in the current solution and replace it with precomputation, hashing, or a tighter iteration strategy.";
        private const string WrongAnswerNextStep =
            "Compare the current output against the expected output on a small case, then inspect boundary conditions like empty input, duplicates, ordering, and off-by-one behavior.";
        private const string DefaultNextStep =
            "Run one minimal sample and one edge case by hand, then adjust the branch or data handling that diverges from the expected behavior.";

        public static string Generate(SubmissionFeedbackContextDto context)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Overall");
            builder.AppendLine(BuildOverallFeedback(context));
            builder.AppendLine();
            builder.AppendLine("Correctness");
            builder.AppendLine(BuildCorrectnessFeedback(context));
            builder.AppendLine();
            builder.AppendLine("Code Quality");
            builder.AppendLine(BuildCodeQualityFeedback(context));
            builder.AppendLine();
            builder.AppendLine("Next Step");
            builder.AppendLine(BuildNextStepFeedback(context));

            return builder.ToString().Trim();
        }

        private static string BuildOverallFeedback(SubmissionFeedbackContextDto context)
        {
            return context.Status switch
            {
                "Accepted" =>
                    context.IsPracticeMode
                        ? AcceptedPracticeOverall
                        : AcceptedInterviewOverall,
                "CompilationError" => CompilationErrorOverall,
                "RuntimeError" => RuntimeErrorOverall,
                "TimeLimitExceeded" => TimeLimitExceededOverall,
                "WrongAnswer" => WrongAnswerOverall,
                _ => DefaultOverall
            };
        }

        private static string BuildCorrectnessFeedback(SubmissionFeedbackContextDto context)
        {
            if (context.TotalTests <= 0)
                return NoTestSummaryCorrectness;

            if (context.Status == "Accepted")
                return $"You passed {context.PassedTests} out of {context.TotalTests} tests. {AcceptedCorrectnessSuffix}";

            var executionOutput = string.IsNullOrWhiteSpace(context.ExecutionOutput)
                ? NoDiagnosticsCorrectness
                : $"Runner diagnostics: {context.ExecutionOutput.Trim()}";

            return $"You passed {context.PassedTests} out of {context.TotalTests} tests. {executionOutput}";
        }

        private static string BuildCodeQualityFeedback(SubmissionFeedbackContextDto context)
        {
            var code = context.SourceCode;
            var hasTodo = code.Contains("TODO", StringComparison.OrdinalIgnoreCase);
            var lineCount = code
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Length;
            var usesLookupStructure =
                code.Contains("Dictionary", StringComparison.Ordinal) ||
                code.Contains("HashSet", StringComparison.Ordinal);
            var likelyNestedLoop =
                CountOccurrences(code, "for") > 1 ||
                CountOccurrences(code, "foreach") > 1 ||
                (code.Contains("for", StringComparison.Ordinal) &&
                 code.Contains("foreach", StringComparison.Ordinal));

            if (hasTodo)
                return TodoCodeQuality;

            if (likelyNestedLoop && !usesLookupStructure)
                return NestedLoopCodeQuality;

            if (usesLookupStructure)
                return LookupStructureCodeQuality;

            if (lineCount > 35)
                return LongMethodCodeQuality;

            return DefaultCodeQuality;
        }

        private static string BuildNextStepFeedback(SubmissionFeedbackContextDto context)
        {
            return context.Status switch
            {
                "Accepted" =>
                    context.IsPracticeMode
                        ? AcceptedPracticeNextStep
                        : AcceptedInterviewNextStep,
                "CompilationError" => CompilationErrorNextStep,
                "RuntimeError" => RuntimeErrorNextStep,
                "TimeLimitExceeded" => TimeLimitExceededNextStep,
                "WrongAnswer" => WrongAnswerNextStep,
                _ => DefaultNextStep
            };
        }

        private static int CountOccurrences(string content, string keyword)
        {
            var count = 0;
            var index = 0;

            while ((index = content.IndexOf(keyword, index, StringComparison.Ordinal)) >= 0)
            {
                count += 1;
                index += keyword.Length;
            }

            return count;
        }
    }
}
