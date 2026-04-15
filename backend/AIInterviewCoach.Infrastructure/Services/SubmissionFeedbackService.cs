using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIInterviewCoach.Infrastructure.Services
{
    public class SubmissionFeedbackService : ISubmissionFeedbackService
    {
        private const string DefaultBaseUrl = "https://api.openai.com/v1";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SubmissionFeedbackService> _logger;

        public SubmissionFeedbackService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SubmissionFeedbackService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SubmissionFeedbackResultDto> GenerateFeedbackAsync(
            SubmissionFeedbackContextDto context,
            CancellationToken cancellationToken = default)
        {
            if (!HasRemoteAiConfiguration())
                return BuildFallbackResult(context);

            try
            {
                var remoteFeedback = await GenerateRemoteFeedbackAsync(context, cancellationToken);

                if (!string.IsNullOrWhiteSpace(remoteFeedback))
                {
                    return new SubmissionFeedbackResultDto
                    {
                        Content = remoteFeedback.Trim(),
                        Source = SubmissionFeedbackSources.OpenAI
                    };
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "AI feedback generation failed for submission on problem '{ProblemTitle}'. Falling back to local feedback.",
                    context.ProblemTitle);
            }

            return BuildFallbackResult(context);
        }

        private bool HasRemoteAiConfiguration()
        {
            return !string.IsNullOrWhiteSpace(GetConfiguredApiKey()) &&
                   !string.IsNullOrWhiteSpace(GetConfiguredModel());
        }

        private async Task<string> GenerateRemoteFeedbackAsync(
            SubmissionFeedbackContextDto context,
            CancellationToken cancellationToken)
        {
            var apiKey = GetConfiguredApiKey()!;
            var model = GetConfiguredModel()!;
            var baseUrl = GetConfiguredBaseUrl();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl.TrimEnd('/')}/responses");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(new
            {
                model,
                instructions =
                    "You are an experienced coding interview coach writing feedback for a compact in-app card. " +
                    "Respond in plain text only. " +
                    "Use exactly these four section titles on their own lines: Overall, Correctness, Code Quality, Next Step. " +
                    "Do not use Markdown headings, bullets, numbering, or code fences. " +
                    "Keep each section to one or two short sentences and keep the whole response concise. " +
                    "Ground every point in the submitted code or execution result. " +
                    "Avoid generic advice about unit tests, comments, input validation, or refactoring unless the code clearly shows that problem. " +
                    "If the submission is accepted, praise briefly and focus on one realistic interview follow-up or one concrete improvement.",
                input = BuildPrompt(context),
                max_output_tokens = 220
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var feedback = ExtractResponseText(document.RootElement);

            if (string.IsNullOrWhiteSpace(feedback))
                throw new InvalidOperationException("AI feedback response did not contain any text.");

            return NormalizeRemoteFeedback(feedback);
        }

        private static string ExtractResponseText(JsonElement responseRoot)
        {
            if (responseRoot.TryGetProperty("output_text", out var outputText) &&
                outputText.ValueKind == JsonValueKind.String)
            {
                return outputText.GetString() ?? string.Empty;
            }

            var builder = new StringBuilder();

            if (!responseRoot.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
                return string.Empty;

            foreach (var item in output.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("content", out var content) ||
                    content.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in content.EnumerateArray())
                {
                    if (contentItem.ValueKind != JsonValueKind.Object ||
                        !contentItem.TryGetProperty("type", out var type) ||
                        !contentItem.TryGetProperty("text", out var text))
                    {
                        continue;
                    }

                    var contentType = type.GetString();
                    if (contentType is "output_text" or "text")
                    {
                        builder.AppendLine(text.GetString());
                    }
                }
            }

            return builder.ToString().Trim();
        }

        private static string NormalizeRemoteFeedback(string feedback)
        {
            var normalizedLines = feedback
                .Replace("\r\n", "\n")
                .Split('\n')
                .Select(line =>
                {
                    var trimmed = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmed))
                        return string.Empty;

                    if (trimmed.StartsWith("#", StringComparison.Ordinal))
                    {
                        trimmed = trimmed.TrimStart('#').Trim();
                    }

                    return trimmed;
                })
                .ToList();

            var builder = new StringBuilder();
            var previousLineWasBlank = false;

            foreach (var line in normalizedLines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    if (builder.Length == 0 || previousLineWasBlank)
                        continue;

                    builder.AppendLine();
                    previousLineWasBlank = true;
                    continue;
                }

                builder.AppendLine(line);
                previousLineWasBlank = false;
            }

            return builder.ToString().Trim();
        }

        private string? GetConfiguredApiKey()
        {
            return GetFirstConfiguredValue(
                _configuration["AiFeedback:ApiKey"],
                _configuration["OPENAI_API_KEY"]);
        }

        private string? GetConfiguredModel()
        {
            return GetFirstConfiguredValue(
                _configuration["AiFeedback:Model"],
                _configuration["OPENAI_MODEL"]);
        }

        private string GetConfiguredBaseUrl()
        {
            return GetFirstConfiguredValue(
                       _configuration["AiFeedback:BaseUrl"],
                       _configuration["OPENAI_BASE_URL"]) ??
                   DefaultBaseUrl;
        }

        private static string? GetFirstConfiguredValue(params string?[] candidates)
        {
            return candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
        }

        private static SubmissionFeedbackResultDto BuildFallbackResult(SubmissionFeedbackContextDto context)
        {
            return new SubmissionFeedbackResultDto
            {
                Content = SubmissionFallbackFeedbackCatalog.Generate(context),
                Source = SubmissionFeedbackSources.LocalFallback
            };
        }

        private static string BuildPrompt(SubmissionFeedbackContextDto context)
        {
            return $"""
                Candidate mode: {(context.IsPracticeMode ? "Practice" : "Interview")}
                Problem title: {context.ProblemTitle}
                Topic: {context.Topic}
                Difficulty: {context.Difficulty}
                Description: {context.ProblemDescription}
                Constraints: {context.ConstraintsText}
                Example input: {context.ExampleInput}
                Example output: {context.ExampleOutput}

                Submission language: {FormatLanguage(context.Language)}
                Submission status: {context.Status}
                Tests passed: {context.PassedTests}/{context.TotalTests}
                Execution output:
                {context.ExecutionOutput}

                Desired feedback style:
                - concise UI card, not an essay
                - mention only issues that are actually visible in this code or result
                - prefer one concrete improvement over a generic checklist

                Source code:
                ```{context.Language}
                {context.SourceCode}
                ```
                """;
        }

        private static string FormatLanguage(string language)
        {
            return language.Trim().ToLowerInvariant() switch
            {
                "cpp" or "c++" => "C++",
                "python" or "py" => "Python",
                "csharp" or "cs" or "c#" => "C#",
                _ => language
            };
        }

    }
}
