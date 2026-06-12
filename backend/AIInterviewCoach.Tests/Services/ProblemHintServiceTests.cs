using System.Net;
using System.Text;
using System.Text.Json;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Infrastructure.Services;
using AIInterviewCoach.Tests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIInterviewCoach.Tests.Services
{
    public class ProblemHintServiceTests
    {
        private const string WellFormedHint = "Hint 1\nTrack the running minimum.";

        private static IConfiguration ConfigWith(Dictionary<string, string?> values) =>
            new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        private static IConfiguration FullConfig(
            string apiKey = "test-key",
            string model = "gpt-4o",
            string? baseUrl = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["AiHints:ApiKey"] = apiKey,
                ["AiHints:Model"] = model,
            };
            if (baseUrl is not null)
                values["AiHints:BaseUrl"] = baseUrl;
            return ConfigWith(values);
        }

        private static ProblemHintContextDto SampleContext(
            int level = 1,
            string title = "Two Sum",
            string language = "csharp",
            string sourceCode = "public int[] TwoSum(int[] nums, int target) { return []; }",
            string topic = "Arrays") =>
            new()
            {
                Level = level,
                ProblemTitle = title,
                ProblemDescription = "Find two numbers that sum to target.",
                Difficulty = "Easy",
                Topic = topic,
                ConstraintsText = "1 <= n <= 100",
                ExampleInput = "[2,7,11,15], target=9",
                ExampleOutput = "[0,1]",
                Language = language,
                SourceCode = sourceCode,
            };

        private static ProblemHintService BuildService(
            FakeHttpMessageHandler handler,
            IConfiguration? configuration = null) =>
            new(
                new HttpClient(handler),
                configuration ?? FullConfig(),
                NullLogger<ProblemHintService>.Instance);

        private static string JsonFor(string hintText) =>
            JsonSerializer.Serialize(new { output_text = hintText });

        // ── Configuration guard ───────────────────────────────────────────────

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenApiKeyIsAbsent()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedHint));
            var svc = BuildService(handler, ConfigWith(new() { ["AiHints:Model"] = "gpt-4o" }));

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenModelIsAbsent()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedHint));
            var svc = BuildService(handler, ConfigWith(new() { ["AiHints:ApiKey"] = "test-key" }));

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task GenerateHintAsync_RecognizesAiFeedbackConfigKeys_WhenAiHintsKeysAbsent()
        {
            // AiFeedback:* is the second priority in the fallback chain
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedHint));
            var config = ConfigWith(new()
            {
                ["AiFeedback:ApiKey"] = "feedback-key",
                ["AiFeedback:Model"] = "gpt-4o",
            });
            var svc = BuildService(handler, config);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("OpenAI", result.Source);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task GenerateHintAsync_RecognizesAlternateEnvVarConfigKeys()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedHint));
            var config = ConfigWith(new()
            {
                ["OPENAI_API_KEY"] = "env-key",
                ["OPENAI_MODEL"] = "gpt-4o",
            });
            var svc = BuildService(handler, config);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("OpenAI", result.Source);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task GenerateHintAsync_PrefersAiHintsApiKey_OverAiFeedbackApiKey()
        {
            string? capturedAuth = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedAuth = req.Headers.Authorization?.ToString();
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedHint), Encoding.UTF8, "application/json")
                });
            });
            var config = ConfigWith(new()
            {
                ["AiHints:ApiKey"] = "hints-key",
                ["AiHints:Model"] = "gpt-4o",
                ["AiFeedback:ApiKey"] = "feedback-key",
            });
            var svc = BuildService(handler, config);

            await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("Bearer hints-key", capturedAuth);
        }

        // ── Success paths ─────────────────────────────────────────────────────

        [Fact]
        public async Task GenerateHintAsync_ReturnsOpenAiContent_WhenResponseUsesOutputTextField()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonFor(WellFormedHint));
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("OpenAI", result.Source);
            Assert.Contains("running minimum", result.Content);
        }

        [Fact]
        public async Task GenerateHintAsync_ReturnsOpenAiContent_WhenResponseUsesOutputArrayFormat()
        {
            var json = """
                {
                  "output": [{
                    "content": [
                      { "type": "output_text", "text": "Hint 2\nUse a hash map." }
                    ]
                  }]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext(level: 2));

            Assert.Equal("OpenAI", result.Source);
            Assert.Contains("hash map", result.Content);
        }

        [Fact]
        public async Task GenerateHintAsync_ReturnsOpenAiContent_WhenResponseUsesTextTypeInArray()
        {
            // type == "text" is accepted in addition to "output_text"
            var json = """
                {
                  "output": [{
                    "content": [
                      { "type": "text", "text": "Hint 1\nThink about state." }
                    ]
                  }]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("OpenAI", result.Source);
        }

        [Fact]
        public async Task GenerateHintAsync_SetsLevelFromContext()
        {
            var handler = FakeHttpMessageHandler.Returning(JsonFor("Hint 3\nCheck edge cases."));
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext(level: 3));

            Assert.Equal(3, result.Level);
        }

        // ── Failure / fallback paths ──────────────────────────────────────────
        //
        // NOTE — Design mirror of SubmissionFeedbackService:
        //   GenerateHintAsync never throws. AI errors are caught internally and
        //   produce a LocalFallback result pointing to ProblemHintFallbackCatalog.

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenResponseBodyHasNoContent()
        {
            var handler = FakeHttpMessageHandler.Returning("{}");
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
            Assert.False(string.IsNullOrWhiteSpace(result.Content));
        }

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenOutputTextIsWhitespaceOnly()
        {
            var handler = FakeHttpMessageHandler.Returning(
                JsonSerializer.Serialize(new { output_text = "   " }));
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
        }

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenHttpCallThrows()
        {
            var handler = FakeHttpMessageHandler.Throwing(
                new HttpRequestException("Network unavailable"));
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
        }

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenHttpReturns500()
        {
            var handler = FakeHttpMessageHandler.Returning(
                """{"error":"Internal Server Error"}""",
                HttpStatusCode.InternalServerError);
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
        }

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenHttpReturns401()
        {
            var handler = FakeHttpMessageHandler.Returning(
                """{"error":"Unauthorized"}""",
                HttpStatusCode.Unauthorized);
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
        }

        // ── NormalizeRemoteHint ───────────────────────────────────────────────

        [Fact]
        public async Task GenerateHintAsync_InjectsHintPrefix_WhenRemoteResponseOmitsIt()
        {
            // Response has no "Hint N" prefix → NormalizeRemoteHint prepends it
            var handler = FakeHttpMessageHandler.Returning(
                JsonFor("Track the running minimum."));
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext(level: 1));

            Assert.StartsWith("Hint 1", result.Content);
        }

        [Fact]
        public async Task GenerateHintAsync_DoesNotDuplicatePrefix_WhenRemoteResponseIncludesIt()
        {
            // Response already has "hint 1" (case-insensitive) → no second prefix injected
            var handler = FakeHttpMessageHandler.Returning(JsonFor("hint 1\nTrack the minimum."));
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext(level: 1));

            Assert.DoesNotContain("Hint 1\nHint 1", result.Content,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GenerateHintAsync_TrimsPerLineWhitespace_FromRemoteHint()
        {
            var handler = FakeHttpMessageHandler.Returning(
                JsonFor("  Hint 1  \n  Track the minimum.  "));
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext(level: 1));

            Assert.DoesNotContain("  ", result.Content);
        }

        [Fact]
        public async Task GenerateHintAsync_RemovesBlankLines_FromRemoteHint()
        {
            // Unlike feedback normalization, NormalizeRemoteHint strips ALL blank lines
            var handler = FakeHttpMessageHandler.Returning(
                JsonFor("Hint 1\n\nTrack the minimum.\n\nStay linear."));
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext(level: 1));

            Assert.DoesNotContain("\n\n", result.Content);
        }

        // ── Prompt building ───────────────────────────────────────────────────

        [Fact]
        public async Task GenerateHintAsync_BuildsPrompt_ContainingContextFields()
        {
            string? capturedBody = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedHint), Encoding.UTF8, "application/json")
                };
            });
            var svc = BuildService(handler);

            await svc.GenerateHintAsync(SampleContext(level: 2, topic: "Arrays"));

            Assert.NotNull(capturedBody);
            Assert.Contains("Two Sum", capturedBody);
            Assert.Contains("Arrays", capturedBody);
            Assert.Contains("Easy", capturedBody);
        }

        [Fact]
        public async Task GenerateHintAsync_BuildsPrompt_WithNoAttemptMessage_WhenSourceCodeIsEmpty()
        {
            string? capturedBody = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedHint), Encoding.UTF8, "application/json")
                };
            });
            var svc = BuildService(handler);

            await svc.GenerateHintAsync(SampleContext(sourceCode: ""));

            Assert.NotNull(capturedBody);
            Assert.Contains("No current attempt yet", capturedBody);
        }

        [Fact]
        public async Task GenerateHintAsync_TruncatesSourceCode_AtFiveThousandChars()
        {
            var longCode = new string('x', 5001);
            string? capturedBody = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedHint), Encoding.UTF8, "application/json")
                };
            });
            var svc = BuildService(handler);

            await svc.GenerateHintAsync(SampleContext(sourceCode: longCode));

            Assert.NotNull(capturedBody);
            Assert.Contains("[truncated]", capturedBody);
        }

        // ── FormatLanguage ────────────────────────────────────────────────────

        [Theory]
        [InlineData("cpp",    "C++")]
        [InlineData("python", "Python")]
        [InlineData("csharp", "C#")]
        [InlineData("rust",   "C#")]    // default arm
        public async Task GenerateHintAsync_FormatsLanguageInPrompt(
            string inputLanguage, string expectedDisplay)
        {
            string? capturedBody = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedHint), Encoding.UTF8, "application/json")
                };
            });
            var svc = BuildService(handler);

            await svc.GenerateHintAsync(SampleContext(language: inputLanguage));

            Assert.NotNull(capturedBody);
            using var doc = JsonDocument.Parse(capturedBody);
            var inputText = doc.RootElement.GetProperty("input").GetString();
            Assert.Contains($"Candidate language: {expectedDisplay}", inputText);
        }

        // ── HTTP request details ──────────────────────────────────────────────

        [Fact]
        public async Task GenerateHintAsync_SendsBearerTokenInAuthorizationHeader()
        {
            string? capturedAuth = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedAuth = req.Headers.Authorization?.ToString();
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedHint), Encoding.UTF8, "application/json")
                });
            });
            var svc = BuildService(handler, FullConfig(apiKey: "my-secret-key"));

            await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("Bearer my-secret-key", capturedAuth);
        }

        [Fact]
        public async Task GenerateHintAsync_UsesCustomBaseUrl_WhenConfigured()
        {
            string? capturedUrl = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedUrl = req.RequestUri!.ToString();
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedHint), Encoding.UTF8, "application/json")
                });
            });
            var svc = BuildService(handler, FullConfig(baseUrl: "https://custom.example.com/v1"));

            await svc.GenerateHintAsync(SampleContext());

            Assert.StartsWith("https://custom.example.com/v1", capturedUrl);
        }

        [Fact]
        public async Task GenerateHintAsync_UsesDefaultOpenAiBaseUrl_WhenNotConfigured()
        {
            string? capturedUrl = null;
            var handler = new FakeHttpMessageHandler(async req =>
            {
                capturedUrl = req.RequestUri!.ToString();
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonFor(WellFormedHint), Encoding.UTF8, "application/json")
                });
            });
            var svc = BuildService(handler); // no BaseUrl in config

            await svc.GenerateHintAsync(SampleContext());

            Assert.StartsWith("https://api.openai.com/v1", capturedUrl);
        }

        // ── ExtractResponseText — uncovered branches ──────────────────────────

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenOutputTextPropertyIsNotAString()
        {
            // output_text is a number → ValueKind guard fails → array path → no array → "" → throws → fallback
            var handler = FakeHttpMessageHandler.Returning("""{"output_text": 42}""");
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
        }

        [Fact]
        public async Task GenerateHintAsync_SkipsNonObjectItemsInOutputArray_AndExtractsValidOnes()
        {
            var json = """
                {
                  "output": [
                    42,
                    {
                      "content": [
                        { "type": "output_text", "text": "Hint 1\nStay close to the constraints." }
                      ]
                    }
                  ]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("OpenAI", result.Source);
            Assert.Contains("constraints", result.Content);
        }

        [Fact]
        public async Task GenerateHintAsync_ReturnsFallback_WhenOutputItemContentIsNotAnArray()
        {
            // content is an object, not an array → guard fires → item skipped → "" → fallback
            var json = """
                {
                  "output": [
                    { "content": { "type": "output_text", "text": "Should be ignored" } }
                  ]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("LocalFallback", result.Source);
        }

        [Fact]
        public async Task GenerateHintAsync_SkipsContentItemsMissingTypeOrTextProperty()
        {
            var json = """
                {
                  "output": [{
                    "content": [
                      { "type": "output_text" },
                      { "text": "orphan" },
                      { "type": "output_text", "text": "Hint 1\nValid content." }
                    ]
                  }]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("OpenAI", result.Source);
            Assert.DoesNotContain("orphan", result.Content);
        }

        [Fact]
        public async Task GenerateHintAsync_SkipsContentItemsWithUnknownType_AndExtractsKnownOnes()
        {
            var json = """
                {
                  "output": [{
                    "content": [
                      { "type": "tool_use", "text": "Should be ignored" },
                      { "type": "output_text", "text": "Hint 1\nValid content." }
                    ]
                  }]
                }
                """;
            var handler = FakeHttpMessageHandler.Returning(json);
            var svc = BuildService(handler);

            var result = await svc.GenerateHintAsync(SampleContext());

            Assert.Equal("OpenAI", result.Source);
            Assert.DoesNotContain("Should be ignored", result.Content);
        }
    }
}
