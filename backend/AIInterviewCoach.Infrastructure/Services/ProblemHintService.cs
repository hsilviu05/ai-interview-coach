using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIInterviewCoach.Infrastructure.Services
{
    public class ProblemHintService : IProblemHintService
    {
        private const string DefaultBaseUrl = "https://api.openai.com/v1";
        private const string OpenAiSource = "OpenAI";
        private const string FallbackSource = "LocalFallback";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProblemHintService> _logger;

        public ProblemHintService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ProblemHintService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ProblemHintResponseDto> GenerateHintAsync(
            ProblemHintContextDto context,
            CancellationToken cancellationToken = default)
        {
            if (!HasRemoteAiConfiguration())
            {
                return BuildFallbackResult(context);
            }

            try
            {
                var remoteHint = await GenerateRemoteHintAsync(context, cancellationToken);
                if (!string.IsNullOrWhiteSpace(remoteHint))
                {
                    return new ProblemHintResponseDto
                    {
                        Level = context.Level,
                        Content = remoteHint.Trim(),
                        Source = OpenAiSource
                    };
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "AI hint generation failed for problem '{ProblemTitle}' at level {HintLevel}. Falling back to local hints.",
                    context.ProblemTitle,
                    context.Level);
            }

            return BuildFallbackResult(context);
        }

        private bool HasRemoteAiConfiguration()
        {
            return !string.IsNullOrWhiteSpace(GetConfiguredApiKey()) &&
                   !string.IsNullOrWhiteSpace(GetConfiguredModel());
        }

        private async Task<string> GenerateRemoteHintAsync(
            ProblemHintContextDto context,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{GetConfiguredBaseUrl().TrimEnd('/')}/responses");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetConfiguredApiKey()!);
            request.Content = JsonContent.Create(new
            {
                model = GetConfiguredModel(),
                instructions =
                    "You are an interview coach giving progressive coding hints. " +
                    "Return plain text only. " +
                    "Do not reveal full code, exact final answers, or full pseudocode. " +
                    "Keep the hint concise, practical, and specific to the problem. " +
                    "Each response must start with 'Hint X' where X is the requested level. " +
                    "Hint 1 should be only a nudge toward the right pattern. " +
                    "Hint 2 can name the algorithm or data structure direction. " +
                    "Hint 3 can mention the critical implementation step and one edge case, but still no full solution.",
                input = BuildPrompt(context),
                max_output_tokens = 170
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var hint = ExtractResponseText(document.RootElement);
            if (string.IsNullOrWhiteSpace(hint))
            {
                throw new InvalidOperationException("AI hint response did not contain any text.");
            }

            return NormalizeRemoteHint(hint, context.Level);
        }

        private string? GetConfiguredApiKey()
        {
            return GetFirstConfiguredValue(
                _configuration["AiHints:ApiKey"],
                _configuration["AiFeedback:ApiKey"],
                _configuration["OPENAI_API_KEY"]);
        }

        private string? GetConfiguredModel()
        {
            return GetFirstConfiguredValue(
                _configuration["AiHints:Model"],
                _configuration["AiFeedback:Model"],
                _configuration["OPENAI_MODEL"]);
        }

        private string GetConfiguredBaseUrl()
        {
            return GetFirstConfiguredValue(
                       _configuration["AiHints:BaseUrl"],
                       _configuration["AiFeedback:BaseUrl"],
                       _configuration["OPENAI_BASE_URL"]) ??
                   DefaultBaseUrl;
        }

        private static ProblemHintResponseDto BuildFallbackResult(ProblemHintContextDto context)
        {
            return new ProblemHintResponseDto
            {
                Level = context.Level,
                Content = ProblemHintFallbackCatalog.Generate(context),
                Source = FallbackSource
            };
        }

        private static string BuildPrompt(ProblemHintContextDto context)
        {
            var sourceCode = string.IsNullOrWhiteSpace(context.SourceCode)
                ? "No current attempt yet."
                : context.SourceCode.Length <= 5000
                    ? context.SourceCode
                    : $"{context.SourceCode[..5000]}\n\n[truncated]";

            return $"""
                Requested hint level: {context.Level}
                Problem title: {context.ProblemTitle}
                Topic: {context.Topic}
                Difficulty: {context.Difficulty}
                Description: {context.ProblemDescription}
                Constraints: {context.ConstraintsText}
                Example input: {context.ExampleInput}
                Example output: {context.ExampleOutput}
                Candidate language: {FormatLanguage(context.Language)}

                Candidate attempt:
                ```{NormalizeCodeFenceLanguage(context.Language)}
                {sourceCode}
                ```

                Hint policy:
                - never provide a full implementation
                - never dump complete pseudocode
                - point to the next useful idea only
                - keep it short enough for an in-app hint card
                """;
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
            {
                return string.Empty;
            }

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

        private static string NormalizeRemoteHint(string hint, int level)
        {
            var normalized = string.Join(
                "\n",
                hint
                    .Replace("\r\n", "\n")
                    .Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line)))
                .Trim();

            var expectedPrefix = $"Hint {level}";
            if (!normalized.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = $"{expectedPrefix}\n{normalized}";
            }

            return normalized;
        }

        private static string FormatLanguage(string language)
        {
            return language.Trim().ToLowerInvariant() switch
            {
                "python" => "Python",
                "cpp" => "C++",
                _ => "C#"
            };
        }

        private static string NormalizeCodeFenceLanguage(string language)
        {
            return language.Trim().ToLowerInvariant() switch
            {
                "python" => "python",
                "cpp" => "cpp",
                _ => "csharp"
            };
        }

        private static string? GetFirstConfiguredValue(params string?[] candidates)
        {
            return candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
        }
    }
}
