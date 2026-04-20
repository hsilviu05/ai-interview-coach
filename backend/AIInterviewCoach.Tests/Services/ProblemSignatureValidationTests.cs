using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;

namespace AIInterviewCoach.Tests.Services
{
    public class ProblemSignatureValidationTests
    {
        [Fact]
        public void ValidateAndNormalize_ShouldTrimWhitespaceAcrossSignatureMetadata()
        {
            var signature = BuildJsonObjectSignature();
            signature.CsharpMethodName = "  Solve  ";
            signature.PythonMethodName = "  solve  ";
            signature.CppMethodName = "  solve  ";
            signature.ReturnType = "  int_array  ";
            signature.Parameters[0].Name = "  nums  ";
            signature.Parameters[0].Type = "  int_array  ";

            var normalized = ProblemSignatureValidation.ValidateAndNormalize(signature);

            Assert.Equal("Solve", normalized.CsharpMethodName);
            Assert.Equal("solve", normalized.PythonMethodName);
            Assert.Equal("solve", normalized.CppMethodName);
            Assert.Equal(ProblemSignatureTypeKeys.IntArray, normalized.ReturnType);
            Assert.Equal("nums", normalized.Parameters[0].Name);
            Assert.Equal(ProblemSignatureTypeKeys.IntArray, normalized.Parameters[0].Type);
        }

        [Fact]
        public void ValidateAndNormalize_ShouldRejectDuplicateParameterNamesIgnoringCase()
        {
            var signature = BuildJsonObjectSignature();
            signature.Parameters.Add(new ProblemSignatureParameterDto
            {
                Name = "NUMS",
                Type = ProblemSignatureTypeKeys.IntArray
            });

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("declared more than once", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_ShouldRejectInvalidMethodIdentifiers()
        {
            var signature = BuildJsonObjectSignature();
            signature.PythonMethodName = "two-sum";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("Python method name 'two-sum' is invalid", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_ShouldRejectTooManyParameters()
        {
            var signature = BuildJsonObjectSignature();
            signature.Parameters = Enumerable.Range(1, 9)
                .Select(index => new ProblemSignatureParameterDto
                {
                    Name = $"value{index}",
                    Type = ProblemSignatureTypeKeys.Int
                })
                .ToList();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("support up to 8 parameters", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_ShouldRejectRawTextSignaturesWithoutSingleStringParameter()
        {
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.RawText,
                CsharpMethodName = "Solve",
                PythonMethodName = "solve",
                CppMethodName = "solve",
                ReturnType = ProblemSignatureTypeKeys.String,
                Parameters =
                [
                    new ProblemSignatureParameterDto
                    {
                        Name = "rawInput",
                        Type = ProblemSignatureTypeKeys.String
                    },
                    new ProblemSignatureParameterDto
                    {
                        Name = "spare",
                        Type = ProblemSignatureTypeKeys.String
                    }
                ]
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("Raw-text signatures must define exactly one string parameter.", exception.Message);
        }

        [Fact]
        public void Generate_ShouldIncludePythonListTypingAndJsonCollectionOutput_ForArraySignatures()
        {
            var signature = BuildJsonObjectSignature();

            var artifacts = ProblemSignatureCodeGenerator.Generate(signature);

            Assert.Contains("from typing import List", artifacts.PythonStarterCode);
            Assert.Contains("from typing import List", artifacts.PythonHarnessCode);
            Assert.Contains("import json", artifacts.PythonHarnessCode);
            Assert.Contains("print(json.dumps(result))", artifacts.PythonHarnessCode);
            Assert.Contains("JsonSerializer.Serialize(result)", artifacts.CsharpHarnessCode);
            Assert.Contains("FormatIntArray(result)", artifacts.CppHarnessCode);
        }

        [Fact]
        public void Generate_ShouldGenerateRawTextHarnessWithoutJsonParsing()
        {
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.RawText,
                CsharpMethodName = "Solve",
                PythonMethodName = "solve",
                CppMethodName = "solve",
                ReturnType = ProblemSignatureTypeKeys.String,
                Parameters =
                [
                    new ProblemSignatureParameterDto
                    {
                        Name = "rawInput",
                        Type = ProblemSignatureTypeKeys.String
                    }
                ]
            };

            var artifacts = ProblemSignatureCodeGenerator.Generate(signature);

            Assert.Contains("Console.In.ReadToEnd()", artifacts.CsharpHarnessCode);
            Assert.DoesNotContain("JsonSerializer.Deserialize", artifacts.CsharpHarnessCode);

            Assert.Contains("raw_input = sys.stdin.read()", artifacts.PythonHarnessCode);
            Assert.DoesNotContain("json.loads", artifacts.PythonHarnessCode);

            Assert.Contains("ReadAllInput()", artifacts.CppHarnessCode);
            Assert.DoesNotContain("ExtractIntField", artifacts.CppHarnessCode);
            Assert.DoesNotContain("ExtractStringArrayField", artifacts.CppHarnessCode);
        }

        [Fact]
        public void Generate_ShouldFormatBooleanOutputsConsistentlyAcrossLanguages()
        {
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                CsharpMethodName = "IsValid",
                PythonMethodName = "isValid",
                CppMethodName = "isValid",
                ReturnType = ProblemSignatureTypeKeys.Bool,
                Parameters =
                [
                    new ProblemSignatureParameterDto
                    {
                        Name = "s",
                        Type = ProblemSignatureTypeKeys.String
                    }
                ]
            };

            var artifacts = ProblemSignatureCodeGenerator.Generate(signature);

            Assert.Contains("? \"true\" : \"false\"", artifacts.CsharpHarnessCode);
            Assert.Contains("print(\"true\" if result else \"false\")", artifacts.PythonHarnessCode);
            Assert.Contains("? \"true\" : \"false\"", artifacts.CppHarnessCode);
        }

        private static ProblemSignatureDefinitionDto BuildJsonObjectSignature() =>
            new()
            {
                InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                CsharpMethodName = "TwoSum",
                PythonMethodName = "twoSum",
                CppMethodName = "twoSum",
                ReturnType = ProblemSignatureTypeKeys.IntArray,
                Parameters =
                [
                    new ProblemSignatureParameterDto
                    {
                        Name = "nums",
                        Type = ProblemSignatureTypeKeys.IntArray
                    },
                    new ProblemSignatureParameterDto
                    {
                        Name = "target",
                        Type = ProblemSignatureTypeKeys.Int
                    }
                ]
            };
    }
}
