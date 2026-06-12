using System.Net;
using System.Text;
using System.Text.Json;
using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Infrastructure.Services;
using AIInterviewCoach.Tests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIInterviewCoach.Tests.Services
{
    public class SubmissionFeedbackServiceTests
    {
        // A well-formed four-section response that passes through NormalizeRemoteFeedback unchanged.
        private const string WellFormedFeedback =
            "Overall\nClean solution.\n\nCorrectness\nAll tests pass.\n\nCode Quality\nGood naming.\n\nNext Step\nTry an O(n) approach.";

        private static IConfiguration ConfigWith(Dictionary<string, string?> values) =>
            new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        private static IConfiguration FullConfig(
            string apiKey = "test-key",
            string model = "gpt-4o",
            string? baseUrl = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["AiFeedback:ApiKey"] = apiKey,
                ["AiFeedback:Model"] = model,
            };
            if (baseUrl is not null)
                values["AiFeedback:BaseUrl"] = baseUrl;
            return ConfigWith(values);
        }

        private static SubmissionFeedbackContextDto SampleContext(bool isPracticeMode = false) =>
            new()
            {
                ProblemTitle = "Two Sum",
                ProblemDescription = "Find two numbers that sum to target.",
                Difficulty = "Easy",
                Topic = "Arrays",
                ConstraintsText = "1 <= n <= 100",
                ExampleInput = "[2,7,11,15], target=9",
                ExampleOutput = "[0,1]",
                Language = "csharp",
                SourceCode = "public int[] TwoSum(int[] nums, int target) { return []; }",
                Status = "Accepted",
                PassedTests = 3,
                TotalTests = 3,
                ExecutionOutput = "Accepted.",
                IsPracticeMode = isPracticeMode,
            };

        private static SubmissionFeedbackService BuildService(
            FakeHttpMessageHandler handler,
            IConfiguration? configuration = null) =>
            new(
                new HttpClient(handler),
                configuration ?? FullConfig(),
                NullLogger<SubmissionFeedbackService>.Instance);

        private static string JsonFor(string feedbackText) =>
            JsonSerializer.Serialize(new { output_text = feedbackText });

        // ── Configuration guard ───────────────────────────────────────────────

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_WhenApiKeyIsAbsent()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedFeedback));
            var svc = BuildService(handler, ConfigWith(new() { ["AiFeedback:Model"] = "gpt-4o" }));

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
            Assert.Empty(handler.Requests); // no HTTP call made
        }

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_WhenModelIsAbsent()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedFeedback));
            var svc = BuildService(handler, ConfigWith(new() { ["AiFeedback:ApiKey"] = "test-key" }));

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_RecognizesAlternateEnvVarConfigKeys()
        {
            // OPENAI_API_KEY / OPENAI_MODEL are the alternative config keys
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedFeedback));
            var config = ConfigWith(new()
            {
                ["OPENAI_API_KEY"] = "env-key",
                ["OPENAI_MODEL"] = "gpt-4o",
            });
            var svc = BuildService(handler, config);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.OpenAI, result.Source);
            Assert.Single(handler.Requests);
        }

        // ── Success paths ─────────────────────────────────────────────────────

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsOpenAiContent_WhenResponseUsesOutputTextField()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedFeedback));
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.OpenAI, result.Source);
            Assert.Contains("Overall", result.Content);
            Assert.Contains("Next Step", result.Content);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsOpenAiContent_WhenResponseUsesOutputArrayFormat()
        {
            var json = """
                {
                  "output": [{
                    "content": [
                      {
                        "type": "output_text",
                        "text": "Overall\nClean.\n\nCorrectness\nAll pass.\n\nCode Quality\nReadable.\n\nNext Step\nOptimize."
                      }
                    ]
                  }]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.OpenAI, result.Source);
            Assert.Contains("Correctness", result.Content);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsOpenAiContent_WhenResponseUsesTextTypeInArray()
        {
            // The extractor also accepts type == "text" (not only "output_text")
            var json = """
                {
                  "output": [{
                    "content": [
                      { "type": "text", "text": "Overall\nLooks good.\n\nCorrectness\nOK.\n\nCode Quality\nFine.\n\nNext Step\nDone." }
                    ]
                  }]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.OpenAI, result.Source);
        }

        // ── Failure / fallback paths ─────────────────────────────────────────
        //
        // NOTE — Design observation:
        //   GenerateFeedbackAsync is designed to *never* throw. When the remote
        //   call fails (network error, non-2xx, empty response), it catches the
        //   exception internally and returns a LocalFallback result instead of
        //   propagating. This means SubmissionFeedbackProcessor's catch block
        //   — which sets AiFeedbackStatus = Failed — is unreachable from AI
        //   errors (only from a null Problem entity). The tests below document
        //   the *actual* contract: errors → fallback, never → throws.

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_AndDoesNotThrow_WhenResponseBodyHasNoContent()
        {
            // {} → ExtractResponseText returns "" → throws InvalidOperationException
            // → caught by GenerateFeedbackAsync → returns fallback
            var handler = FakeHttpMessageHandler.Returning("{}");
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
            Assert.False(string.IsNullOrWhiteSpace(result.Content));
        }

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_AndDoesNotThrow_WhenOutputTextIsWhitespaceOnly()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonSerializer.Serialize(new { output_text = "   " }));
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_AndDoesNotThrow_WhenHttpCallThrows()
        {
            var handler = FakeHttpMessageHandler.Throwing(new HttpRequestException("Network unavailable"));
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_AndDoesNotThrow_WhenHttpReturns500()
        {
            var handler = FakeHttpMessageHandler.Returning(
                """{"error":"Internal Server Error"}""",
                HttpStatusCode.InternalServerError);
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_AndDoesNotThrow_WhenHttpReturns401()
        {
            var handler = FakeHttpMessageHandler.Returning(
                """{"error":"Unauthorized"}""",
                HttpStatusCode.Unauthorized);
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
        }

        // ── Prompt building ───────────────────────────────────────────────────

        [Fact]
        public async Task GenerateFeedbackAsync_BuildsPrompt_ContainingContextFields()
        {
            string? capturedBody = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                // Read content inside the handler while the request is still alive
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedFeedback), Encoding.UTF8, "application/json")
                };
            });
            var svc = BuildService(handler);

            await svc.GenerateFeedbackAsync(SampleContext());

            Assert.NotNull(capturedBody);
            Assert.Contains("Two Sum", capturedBody);
            Assert.Contains("Arrays", capturedBody);
            Assert.Contains("Easy", capturedBody);
            Assert.Contains("csharp", capturedBody);
            Assert.Contains("Accepted", capturedBody);
            Assert.Contains("Candidate mode: Interview", capturedBody);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_BuildsPrompt_WithPracticeModeFlagSet()
        {
            string? capturedBody = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedFeedback), Encoding.UTF8, "application/json")
                };
            });
            var svc = BuildService(handler);

            await svc.GenerateFeedbackAsync(SampleContext(isPracticeMode: true));

            Assert.Contains("Candidate mode: Practice", capturedBody);
        }

        // ── Normalization ─────────────────────────────────────────────────────

        [Fact]
        public async Task GenerateFeedbackAsync_StripsMarkdownHeadings_FromRemoteResponse()
        {
            var markdownResponse = "# Overall\nGood.\n\n## Correctness\nAll pass.\n\n### Code Quality\nClean.\n\nNext Step\nOptimize.";
            var handler = FakeHttpMessageHandler.Returning(JsonSerializer.Serialize(new { output_text = markdownResponse }));
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.DoesNotContain("##", result.Content);
            Assert.DoesNotContain("# ", result.Content);
            Assert.DoesNotContain("#O", result.Content); // no run-together '#Overall'
            Assert.Contains("Overall", result.Content);
            Assert.Contains("Correctness", result.Content);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_CollapsesConsecutiveBlankLines_FromRemoteResponse()
        {
            var bloated = "Overall\nGood.\n\n\n\nCorrectness\nFine.\n\nCode Quality\nClean.\n\nNext Step\nDone.";
            var handler = FakeHttpMessageHandler.Returning(JsonSerializer.Serialize(new { output_text = bloated }));
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            // Three or more consecutive newlines should not appear after normalization
            Assert.DoesNotContain("\n\n\n", result.Content);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_TrimsLeadingAndTrailingWhitespace_FromRemoteResponse()
        {
            var padded = "\n\n  Overall\nGood.\n\nCorrectness\nFine.\n\nCode Quality\nClean.\n\nNext Step\nDone.\n\n  ";
            var handler = FakeHttpMessageHandler.Returning(JsonSerializer.Serialize(new { output_text = padded }));
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.False(result.Content.StartsWith("\n") || result.Content.StartsWith(" "),
                "Content should not start with whitespace.");
            Assert.False(result.Content.EndsWith("\n") || result.Content.EndsWith(" "),
                "Content should not end with whitespace.");
        }

        // ── ExtractResponseText — uncovered branches ─────────────────────────

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_WhenOutputTextPropertyIsNotAString()
        {
            // output_text exists but its ValueKind is Number, not String.
            // The guard `outputText.ValueKind == JsonValueKind.String` is false, so
            // ExtractResponseText falls through to the output-array path. The array is
            // absent, so it returns "". The empty-string check then throws
            // InvalidOperationException, which GenerateFeedbackAsync catches → LocalFallback.
            var handler = FakeHttpMessageHandler.Returning("""{"output_text": 42}""");
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_SkipsNonObjectItemsInOutputArray_AndExtractsValidOnes()
        {
            // A primitive (42) at output[0] fails the `item.ValueKind != JsonValueKind.Object`
            // guard and is skipped. The valid object at output[1] is still extracted.
            var json = """
                {
                  "output": [
                    42,
                    {
                      "content": [
                        { "type": "output_text", "text": "Overall\nGood.\n\nCorrectness\nOK.\n\nCode Quality\nFine.\n\nNext Step\nDone." }
                      ]
                    }
                  ]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.OpenAI, result.Source);
            Assert.Contains("Overall", result.Content);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_ReturnsLocalFallback_WhenOutputItemContentIsNotAnArray()
        {
            // item.content is a JSON object (not an array). The guard
            // `content.ValueKind != JsonValueKind.Array` fires → item skipped.
            // No text is extracted → empty string → throws → LocalFallback.
            var json = """
                {
                  "output": [
                    { "content": { "type": "output_text", "text": "Should be ignored" } }
                  ]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.LocalFallback, result.Source);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_SkipsContentItemsMissingTypeOrTextProperty()
        {
            // Items that lack "type" or lack "text" hit the inner `continue` guard.
            // The last item has both and is extracted normally.
            var json = """
                {
                  "output": [{
                    "content": [
                      { "type": "output_text" },
                      { "text": "orphan" },
                      { "type": "output_text", "text": "Overall\nGood.\n\nCorrectness\nOK.\n\nCode Quality\nFine.\n\nNext Step\nDone." }
                    ]
                  }]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.OpenAI, result.Source);
            Assert.DoesNotContain("orphan", result.Content);
            Assert.Contains("Overall", result.Content);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_SkipsContentItemsWithUnknownType_AndExtractsKnownOnes()
        {
            // type == "tool_use" is neither "output_text" nor "text" → not appended.
            // The following "output_text" item is still extracted.
            var json = """
                {
                  "output": [{
                    "content": [
                      { "type": "tool_use", "text": "Should be ignored" },
                      { "type": "output_text", "text": "Overall\nGood.\n\nCorrectness\nOK.\n\nCode Quality\nFine.\n\nNext Step\nDone." }
                    ]
                  }]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal(SubmissionFeedbackSources.OpenAI, result.Source);
            Assert.DoesNotContain("Should be ignored", result.Content);
            Assert.Contains("Overall", result.Content);
        }

        // ── FormatLanguage — uncovered arms ───────────────────────────────────

        [Theory]
        [InlineData("cpp",    "C++")]
        [InlineData("c++",    "C++")]
        [InlineData("python", "Python")]
        [InlineData("py",     "Python")]
        [InlineData("cs",     "C#")]
        [InlineData("c#",     "C#")]
        [InlineData("rust",   "rust")]  // default: passthrough unchanged
        public async Task GenerateFeedbackAsync_FormatsLanguageInPrompt(string inputLanguage, string expectedDisplay)
        {
            string? capturedBody = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedFeedback), Encoding.UTF8, "application/json")
                };
            });
            var context = SampleContext();
            context.Language = inputLanguage;
            var svc = BuildService(handler);

            await svc.GenerateFeedbackAsync(context);

            Assert.NotNull(capturedBody);
            using var doc = JsonDocument.Parse(capturedBody);
            var inputText = doc.RootElement.GetProperty("input").GetString();
            Assert.Contains($"Submission language: {expectedDisplay}", inputText);
        }

        // ── HTTP request details ──────────────────────────────────────────────

        [Fact]
        public async Task GenerateFeedbackAsync_SendsBearerTokenInAuthorizationHeader()
        {
            string? capturedAuth = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedAuth = req.Headers.Authorization?.ToString();
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedFeedback), Encoding.UTF8, "application/json")
                });
            });
            var svc = BuildService(handler, FullConfig(apiKey: "my-secret-key"));

            await svc.GenerateFeedbackAsync(SampleContext());

            Assert.Equal("Bearer my-secret-key", capturedAuth);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_UsesCustomBaseUrl_WhenConfigured()
        {
            string? capturedUrl = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedUrl = req.RequestUri!.ToString();
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedFeedback), Encoding.UTF8, "application/json")
                });
            });
            var svc = BuildService(handler, FullConfig(baseUrl: "https://custom.example.com/v1"));

            await svc.GenerateFeedbackAsync(SampleContext());

            Assert.StartsWith("https://custom.example.com/v1", capturedUrl);
        }

        [Fact]
        public async Task GenerateFeedbackAsync_UsesDefaultOpenAiBaseUrl_WhenNotConfigured()
        {
            string? capturedUrl = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedUrl = req.RequestUri!.ToString();
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedFeedback), Encoding.UTF8, "application/json")
                });
            });
            var svc = BuildService(handler); // no BaseUrl in config

            await svc.GenerateFeedbackAsync(SampleContext());

            Assert.StartsWith("https://api.openai.com/v1", capturedUrl);
        }
    }
}
