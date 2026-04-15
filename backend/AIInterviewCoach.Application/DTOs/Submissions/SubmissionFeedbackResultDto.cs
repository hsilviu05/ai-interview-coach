namespace AIInterviewCoach.Application.DTOs.Submissions
{
    public static class SubmissionFeedbackSources
    {
        public const string OpenAI = "OpenAI";
        public const string LocalFallback = "Local fallback";
    }

    public sealed class SubmissionFeedbackResultDto
    {
        public string Content { get; init; } = string.Empty;
        public string Source { get; init; } = SubmissionFeedbackSources.LocalFallback;
    }

    public static class SubmissionFeedbackSourceResolver
    {
        public static string? ResolveSource(string? feedback)
        {
            if (string.IsNullOrWhiteSpace(feedback))
                return null;

            return LooksLikeLocalFallback(feedback)
                ? SubmissionFeedbackSources.LocalFallback
                : SubmissionFeedbackSources.OpenAI;
        }

        public static bool LooksLikeLocalFallback(string feedback)
        {
            if (string.IsNullOrWhiteSpace(feedback))
                return false;

            var normalized = feedback.Replace("\r\n", "\n").Trim();

            var hasFallbackShape =
                normalized.StartsWith("Overall\n", StringComparison.Ordinal) &&
                normalized.Contains("\n\nCorrectness\n", StringComparison.Ordinal) &&
                normalized.Contains("\n\nCode Quality\n", StringComparison.Ordinal) &&
                normalized.Contains("\n\nNext Step\n", StringComparison.Ordinal);

            return hasFallbackShape &&
                SubmissionFallbackFeedbackCatalog.KnownFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
        }
    }
}
