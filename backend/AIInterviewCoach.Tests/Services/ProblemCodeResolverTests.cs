using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Tests.Services
{
    public class ProblemCodeResolverTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static string ValidSignatureJson() =>
            ProblemSignatureSerializer.Serialize(new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                CsharpMethodName = "TwoSum",
                PythonMethodName = "twoSum",
                CppMethodName = "twoSum",
                ReturnType = ProblemSignatureTypeKeys.IntArray,
                Parameters =
                [
                    new ProblemSignatureParameterDto { Name = "nums", Type = ProblemSignatureTypeKeys.IntArray },
                    new ProblemSignatureParameterDto { Name = "target", Type = ProblemSignatureTypeKeys.Int }
                ]
            });

        // ── ResolveHarnessTemplate — no signature JSON ────────────────────────

        [Fact]
        public void ResolveHarnessTemplate_ReturnsNull_WhenProblemIsNotFunctionSignatureMode()
        {
            // ExecutionMode != FunctionSignature + no signature JSON → returns null
            var problem = new Problem
            {
                ExecutionMode = ProblemExecutionModes.Stdin,
                CsharpHarnessTemplate = "some harness"
            };

            var result = ProblemCodeResolver.ResolveHarnessTemplate(problem, "csharp");

            Assert.Null(result);
        }

        [Theory]
        [InlineData("csharp", "csharp-harness")]
        [InlineData("python", "python-harness")]
        [InlineData("cpp",    "cpp-harness")]
        public void ResolveHarnessTemplate_ReturnsStoredTemplate_WhenFunctionSignatureModeAndNoSignatureJson(
            string language, string expectedTemplate)
        {
            var problem = new Problem
            {
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                SignatureDefinitionJson = null,
                CsharpHarnessTemplate = "csharp-harness",
                PythonHarnessTemplate = "python-harness",
                CppHarnessTemplate = "cpp-harness",
            };

            var result = ProblemCodeResolver.ResolveHarnessTemplate(problem, language);

            Assert.Equal(expectedTemplate, result);
        }

        [Fact]
        public void ResolveHarnessTemplate_ReturnsNull_ForUnknownLanguage_WhenNoSignatureJson()
        {
            // The stored-template switch has `_ => null` for unknown languages
            var problem = new Problem
            {
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                SignatureDefinitionJson = null,
                CsharpHarnessTemplate = "harness"
            };

            var result = ProblemCodeResolver.ResolveHarnessTemplate(problem, "brainfuck");

            Assert.Null(result);
        }

        // ── ResolveHarnessTemplate — with valid signature JSON ────────────────

        [Fact]
        public void ResolveHarnessTemplate_ReturnsEmpty_ForUnknownLanguage_WhenSignaturePresent()
        {
            // ResolveGeneratedHarnessTemplate `_ => string.Empty` arm
            var problem = new Problem
            {
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                SignatureDefinitionJson = ValidSignatureJson()
            };

            var result = ProblemCodeResolver.ResolveHarnessTemplate(problem, "brainfuck");

            Assert.Equal(string.Empty, result);
        }

        // ── ResolveVisibleStarterCode ─────────────────────────────────────────

        [Fact]
        public void ResolveVisibleStarterCode_ReturnsEmpty_ForUnknownLanguage_WhenSignaturePresent()
        {
            // ResolveGeneratedStarterCode `_ => string.Empty` arm
            var problem = new Problem
            {
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                SignatureDefinitionJson = ValidSignatureJson()
            };

            var result = ProblemCodeResolver.ResolveVisibleStarterCode(problem, "brainfuck");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ResolveVisibleStarterCode_ReturnsEmpty_ForUnknownLanguage_WhenNoSignatureJson()
        {
            // ResolveStoredStarterCode `_ => string.Empty` arm
            var problem = new Problem
            {
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                SignatureDefinitionJson = null,
                CsharpStarterCode = "csharp-starter",
            };

            var result = ProblemCodeResolver.ResolveVisibleStarterCode(problem, "brainfuck");

            Assert.Equal(string.Empty, result);
        }
    }
}
