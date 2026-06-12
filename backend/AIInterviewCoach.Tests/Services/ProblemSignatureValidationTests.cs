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

        // ── ValidateAndNormalize — missing validation branches ───────────────

        [Fact]
        public void ValidateAndNormalize_Throws_WhenSignatureIsNull()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(null));

            Assert.Contains("structured signature metadata", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_Throws_WhenRequiredFieldIsBlank()
        {
            var signature = BuildJsonObjectSignature();
            signature.CsharpMethodName = "   ";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("required blank value", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_Throws_WhenInputBindingModeIsUnsupported()
        {
            var signature = BuildJsonObjectSignature();
            signature.InputBindingMode = "garbage_mode";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("Unsupported input binding mode", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_Throws_WhenReturnTypeIsUnsupported()
        {
            var signature = BuildJsonObjectSignature();
            signature.ReturnType = "float";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("Unsupported return type", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_Throws_WhenParameterTypeIsUnsupported()
        {
            var signature = BuildJsonObjectSignature();
            signature.Parameters[0].Type = "float";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("Unsupported parameter type", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_Throws_WhenParameterNameIsInvalidIdentifier()
        {
            var signature = BuildJsonObjectSignature();
            signature.Parameters[0].Name = "my-param";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("parameter name 'my-param' is invalid", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_Throws_WhenCsharpMethodNameIsInvalidIdentifier()
        {
            var signature = BuildJsonObjectSignature();
            signature.CsharpMethodName = "two-sum";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("C# method name 'two-sum' is invalid", exception.Message);
        }

        [Fact]
        public void ValidateAndNormalize_Throws_WhenCppMethodNameIsInvalidIdentifier()
        {
            var signature = BuildJsonObjectSignature();
            signature.CppMethodName = "two-sum";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ProblemSignatureValidation.ValidateAndNormalize(signature));

            Assert.Contains("C++ method name 'two-sum' is invalid", exception.Message);
        }

        // ── Generate — missing type arms ──────────────────────────────────────

        [Fact]
        public void Generate_StringReturnType_EmitsCorrectTypeMappingsAcrossAllLanguages()
        {
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                CsharpMethodName = "Solve",
                PythonMethodName = "solve",
                CppMethodName = "solve",
                ReturnType = ProblemSignatureTypeKeys.String,
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

            // C# starter: MapCsharpType("string") + BuildCsharpDefaultReturnStatement("string")
            Assert.Contains("string Solve", artifacts.CsharpStarterCode);
            Assert.Contains("return string.Empty;", artifacts.CsharpStarterCode);
            // C# harness: BuildCsharpOutputStatement("string") uses the String arm
            Assert.Contains("result ?? string.Empty", artifacts.CsharpHarnessCode);

            // Python starter: MapPythonType("string") + BuildPythonDefaultReturnStatement("string")
            Assert.Contains("def solve(self, s: str) -> str", artifacts.PythonStarterCode);
            Assert.Contains("return \"\"", artifacts.PythonStarterCode);
            // Python harness: BuildPythonOutputStatement("string") hits the _ arm
            Assert.Contains("print(result)", artifacts.PythonHarnessCode);

            // C++ starter: MapCppReturnType("string") + MapCppParameterType("string")
            Assert.Contains("string solve", artifacts.CppStarterCode);
            Assert.Contains("return \"\";", artifacts.CppStarterCode);
            // C++ harness: BuildCppOutputStatement("string") + BuildCppInputExtraction for string param
            Assert.Contains("cout << result", artifacts.CppHarnessCode);
            Assert.Contains("ExtractStringField", artifacts.CppHarnessCode);
        }

        [Fact]
        public void Generate_IntReturnType_EmitsCsharpDefaultOutputStatement()
        {
            // BuildCsharpOutputStatement "int" falls to the `_` arm → Console.WriteLine(result);
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                CsharpMethodName = "Solve",
                PythonMethodName = "solve",
                CppMethodName = "solve",
                ReturnType = ProblemSignatureTypeKeys.Int,
                Parameters = []
            };

            var artifacts = ProblemSignatureCodeGenerator.Generate(signature);

            Assert.Contains("Console.WriteLine(result);", artifacts.CsharpHarnessCode);
            Assert.Contains("return 0;", artifacts.CsharpStarterCode);
        }

        [Fact]
        public void Generate_StringArrayReturnType_EmitsCorrectTypeMappingsAcrossAllLanguages()
        {
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                CsharpMethodName = "Solve",
                PythonMethodName = "solve",
                CppMethodName = "solve",
                ReturnType = ProblemSignatureTypeKeys.StringArray,
                Parameters =
                [
                    new ProblemSignatureParameterDto
                    {
                        Name = "words",
                        Type = ProblemSignatureTypeKeys.StringArray
                    }
                ]
            };

            var artifacts = ProblemSignatureCodeGenerator.Generate(signature);

            // C# starter: MapCsharpType("string_array") + BuildCsharpDefaultReturnStatement("string_array")
            Assert.Contains("string[]", artifacts.CsharpStarterCode);
            Assert.Contains("return new string[0];", artifacts.CsharpStarterCode);
            // C# harness payload: MapCsharpPayloadType + BuildCsharpPayloadDefaultValue
            Assert.Contains("string[]?", artifacts.CsharpHarnessCode);
            Assert.Contains("Array.Empty<string>()", artifacts.CsharpHarnessCode);

            // Python starter: MapPythonType("string_array")
            Assert.Contains("List[str]", artifacts.PythonStarterCode);
            // Python harness: BuildPythonDefaultValueExpression("string_array") + output
            Assert.Contains("print(json.dumps(result))", artifacts.PythonHarnessCode);

            // C++: MapCppReturnType/ParameterType("string_array")
            Assert.Contains("vector<string>", artifacts.CppStarterCode);
            Assert.Contains("const vector<string>&", artifacts.CppStarterCode);
            Assert.Contains("ExtractStringArrayField", artifacts.CppHarnessCode);
            Assert.Contains("FormatStringArray", artifacts.CppHarnessCode);
        }

        [Fact]
        public void Generate_RawTextWithIntArrayReturn_EmitsFormatIntArrayHelper_InCppHarness()
        {
            // BuildCppOutputHelpers("int_array") in RawText mode inlines FormatIntArray helper
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.RawText,
                CsharpMethodName = "Solve",
                PythonMethodName = "solve",
                CppMethodName = "solve",
                ReturnType = ProblemSignatureTypeKeys.IntArray,
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

            Assert.Contains("FormatIntArray", artifacts.CppHarnessCode);
            Assert.DoesNotContain("ExtractIntArrayField", artifacts.CppHarnessCode);
        }

        [Fact]
        public void Generate_RawTextWithStringArrayReturn_EmitsFormatStringArrayAndEscapeHelper_InCppHarness()
        {
            // BuildCppOutputHelpers("string_array") in RawText mode inlines both helpers
            var signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.RawText,
                CsharpMethodName = "Solve",
                PythonMethodName = "solve",
                CppMethodName = "solve",
                ReturnType = ProblemSignatureTypeKeys.StringArray,
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

            Assert.Contains("FormatStringArray", artifacts.CppHarnessCode);
            Assert.Contains("EscapeJsonString", artifacts.CppHarnessCode);
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
