using System.Text;
using AIInterviewCoach.Application.DTOs.Problems;

namespace AIInterviewCoach.Application.Services.ProblemSignatures
{
    public static class ProblemSignatureCodeGenerator
    {
        public static ProblemSignatureCodeArtifacts Generate(ProblemSignatureDefinitionDto signature)
        {
            var normalized = ProblemSignatureValidation.ValidateAndNormalize(signature);

            return new ProblemSignatureCodeArtifacts(
                CsharpStarterCode: GenerateCsharpStarter(normalized),
                PythonStarterCode: GeneratePythonStarter(normalized),
                CppStarterCode: GenerateCppStarter(normalized),
                CsharpHarnessCode: GenerateCsharpHarnessTemplate(normalized),
                PythonHarnessCode: GeneratePythonHarnessTemplate(normalized),
                CppHarnessCode: GenerateCppHarnessTemplate(normalized));
        }

        private static string GenerateCsharpStarter(ProblemSignatureDefinitionDto signature)
        {
            return $$"""
                public class Solution
                {
                    public {{MapCsharpType(signature.ReturnType)}} {{signature.CsharpMethodName}}({{BuildCsharpParameterList(signature.Parameters)}})
                    {
                        {{BuildCsharpDefaultReturnStatement(signature.ReturnType)}}
                    }
                }
                """;
        }

        private static string GeneratePythonStarter(ProblemSignatureDefinitionDto signature)
        {
            var builder = new StringBuilder();

            if (signature.Parameters.Any(UsesPythonListType) || UsesPythonListType(signature.ReturnType))
            {
                builder.AppendLine("from typing import List");
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine("class Solution:");
            builder.Append("    def ");
            builder.Append(signature.PythonMethodName);
            builder.Append("(self");

            foreach (var parameter in signature.Parameters)
            {
                builder.Append(", ");
                builder.Append(parameter.Name);
                builder.Append(": ");
                builder.Append(MapPythonType(parameter.Type));
            }

            builder.Append(") -> ");
            builder.Append(MapPythonType(signature.ReturnType));
            builder.AppendLine(":");
            builder.Append("        ");
            builder.AppendLine(BuildPythonDefaultReturnStatement(signature.ReturnType));

            return builder.ToString().TrimEnd();
        }

        private static string GenerateCppStarter(ProblemSignatureDefinitionDto signature)
        {
            return $$"""
                {{BuildCppStarterIncludes(signature)}}
                using namespace std;

                class Solution {
                public:
                    {{MapCppReturnType(signature.ReturnType)}} {{signature.CppMethodName}}({{BuildCppParameterList(signature.Parameters)}}) {
                        {{BuildCppDefaultReturnStatement(signature.ReturnType)}}
                    }
                };
                """;
        }

        private static string GenerateCsharpHarnessTemplate(ProblemSignatureDefinitionDto signature)
        {
            if (signature.InputBindingMode == ProblemSignatureInputBindingModes.RawText)
            {
                return $$$"""
                    using System;

                    var rawInput = Console.In.ReadToEnd();
                    var result = new Solution().{{{signature.CsharpMethodName}}}(rawInput);
                    {{{BuildCsharpOutputStatement(signature.ReturnType, "result")}}}

                    {{candidate_code}}
                    """;
            }

            return $$$"""
                using System;
                using System.Text.Json;

                var payload = JsonSerializer.Deserialize<GeneratedInputPayload>(Console.In.ReadToEnd());

                if (payload is null)
                {
                    throw new InvalidOperationException("Invalid input.");
                }

                var result = new Solution().{{{signature.CsharpMethodName}}}({{{BuildCsharpInvocationArguments(signature.Parameters)}}});
                {{{BuildCsharpOutputStatement(signature.ReturnType, "result")}}}

                public sealed class GeneratedInputPayload
                {
                {{{BuildCsharpInputPayloadProperties(signature.Parameters)}}}
                }

                {{candidate_code}}
                """;
        }

        private static string GeneratePythonHarnessTemplate(ProblemSignatureDefinitionDto signature)
        {
            var builder = new StringBuilder();
            var requiresJson = signature.InputBindingMode == ProblemSignatureInputBindingModes.JsonObject || IsCollectionType(signature.ReturnType);
            var requiresListTyping = signature.Parameters.Any(UsesPythonListType) || UsesPythonListType(signature.ReturnType);

            if (requiresJson)
            {
                builder.AppendLine("import json");
            }

            builder.AppendLine("import sys");

            if (requiresListTyping)
            {
                builder.AppendLine("from typing import List");
            }

            builder.AppendLine();
            builder.AppendLine("{{candidate_code}}");
            builder.AppendLine();

            if (signature.InputBindingMode == ProblemSignatureInputBindingModes.RawText)
            {
                builder.AppendLine("raw_input = sys.stdin.read()");
                builder.Append("result = Solution().");
                builder.Append(signature.PythonMethodName);
                builder.AppendLine("(raw_input)");
            }
            else
            {
                builder.AppendLine("payload = json.loads(sys.stdin.read() or \"{}\")");
                builder.Append("result = Solution().");
                builder.Append(signature.PythonMethodName);
                builder.Append('(');
                builder.Append(string.Join(
                    ", ",
                    signature.Parameters.Select(parameter =>
                        $"payload.get({QuotePythonString(parameter.Name)}, {BuildPythonDefaultValueExpression(parameter.Type)})")));
                builder.AppendLine(")");
            }

            builder.AppendLine(BuildPythonOutputStatement(signature.ReturnType));

            return builder.ToString().TrimEnd();
        }

        private static string GenerateCppHarnessTemplate(ProblemSignatureDefinitionDto signature)
        {
            if (signature.InputBindingMode == ProblemSignatureInputBindingModes.RawText)
            {
                var rawTextHelpers = BuildCppOutputHelpers(signature.ReturnType);

                return $$$"""
                    #include <iostream>
                    #include <iterator>
                    #include <string>
                    #include <vector>

                    {{candidate_code}}

                    using namespace std;

                    static string ReadAllInput() {
                        return string(
                            (istreambuf_iterator<char>(cin)),
                            istreambuf_iterator<char>());
                    }

                    {{{rawTextHelpers}}}

                    int main() {
                        const string input = ReadAllInput();
                        Solution solution;
                        auto result = solution.{{{signature.CppMethodName}}}(input);
                        {{{BuildCppOutputStatement(signature.ReturnType, "result")}}}
                        return 0;
                    }
                    """;
            }

            var invocationArguments = signature.InputBindingMode == ProblemSignatureInputBindingModes.RawText
                ? "input"
                : string.Join(
                    ",\n        ",
                    signature.Parameters.Select(parameter => $"{BuildCppInputExtraction(parameter)}"));

            return $$$"""
                #include <cctype>
                #include <iostream>
                #include <iterator>
                #include <sstream>
                #include <string>
                #include <vector>

                {{candidate_code}}

                static string ReadAllInput() {
                    return string(
                        (istreambuf_iterator<char>(cin)),
                        istreambuf_iterator<char>());
                }

                static size_t FindFieldValueStart(const string& input, const string& key) {
                    const auto keyPos = input.find("\"" + key + "\"");
                    if (keyPos == string::npos) {
                        return string::npos;
                    }

                    const auto colon = input.find(':', keyPos + key.size());
                    if (colon == string::npos) {
                        return string::npos;
                    }

                    auto valueStart = colon + 1;
                    while (valueStart < input.size() && isspace(static_cast<unsigned char>(input[valueStart]))) {
                        ++valueStart;
                    }

                    return valueStart;
                }

                static string ExtractStringField(const string& input, const string& key) {
                    const auto valueStart = FindFieldValueStart(input, key);
                    if (valueStart == string::npos || valueStart >= input.size() || input[valueStart] != '"') {
                        return "";
                    }

                    string value;
                    for (size_t index = valueStart + 1; index < input.size(); ++index) {
                        const auto current = input[index];

                        if (current == '\\' && index + 1 < input.size()) {
                            value += input[index + 1];
                            ++index;
                            continue;
                        }

                        if (current == '"') {
                            return value;
                        }

                        value += current;
                    }

                    return value;
                }

                static int ExtractIntField(const string& input, const string& key) {
                    const auto valueStart = FindFieldValueStart(input, key);
                    if (valueStart == string::npos) {
                        return 0;
                    }

                    size_t valueEnd = valueStart;
                    while (valueEnd < input.size() && input[valueEnd] != ',' && input[valueEnd] != '}') {
                        ++valueEnd;
                    }

                    return stoi(input.substr(valueStart, valueEnd - valueStart));
                }

                static bool ExtractBoolField(const string& input, const string& key) {
                    const auto valueStart = FindFieldValueStart(input, key);
                    if (valueStart == string::npos) {
                        return false;
                    }

                    return input.compare(valueStart, 4, "true") == 0;
                }

                static string ExtractArrayBody(const string& input, const string& key) {
                    const auto valueStart = FindFieldValueStart(input, key);
                    if (valueStart == string::npos || valueStart >= input.size() || input[valueStart] != '[') {
                        return "";
                    }

                    auto depth = 0;
                    for (size_t index = valueStart; index < input.size(); ++index) {
                        if (input[index] == '[') {
                            ++depth;
                        } else if (input[index] == ']') {
                            --depth;
                            if (depth == 0) {
                                return input.substr(valueStart + 1, index - valueStart - 1);
                            }
                        }
                    }

                    return "";
                }

                static vector<int> ExtractIntArrayField(const string& input, const string& key) {
                    const auto body = ExtractArrayBody(input, key);
                    if (body.empty()) {
                        return {};
                    }

                    vector<int> values;
                    string token;
                    stringstream stream(body);

                    while (getline(stream, token, ',')) {
                        auto start = token.find_first_not_of(" \t\r\n");
                        if (start == string::npos) {
                            continue;
                        }

                        auto end = token.find_last_not_of(" \t\r\n");
                        values.push_back(stoi(token.substr(start, end - start + 1)));
                    }

                    return values;
                }

                static vector<string> ExtractStringArrayField(const string& input, const string& key) {
                    const auto body = ExtractArrayBody(input, key);
                    if (body.empty()) {
                        return {};
                    }

                    vector<string> values;
                    string current;
                    bool insideString = false;

                    for (size_t index = 0; index < body.size(); ++index) {
                        const auto ch = body[index];
                        if (!insideString) {
                            if (ch == '"') {
                                insideString = true;
                                current.clear();
                            }

                            continue;
                        }

                        if (ch == '\\' && index + 1 < body.size()) {
                            current += body[index + 1];
                            ++index;
                            continue;
                        }

                        if (ch == '"') {
                            values.push_back(current);
                            insideString = false;
                            continue;
                        }

                        current += ch;
                    }

                    return values;
                }

                static string EscapeJsonString(const string& value) {
                    string escaped;
                    escaped.reserve(value.size());

                    for (const auto ch : value) {
                        if (ch == '\\' || ch == '"') {
                            escaped += '\\';
                        }

                        escaped += ch;
                    }

                    return escaped;
                }

                static string FormatStringArray(const vector<string>& values) {
                    string output = "[";

                    for (size_t index = 0; index < values.size(); ++index) {
                        if (index > 0) {
                            output += ",";
                        }

                        output += "\"";
                        output += EscapeJsonString(values[index]);
                        output += "\"";
                    }

                    output += "]";
                    return output;
                }

                static string FormatIntArray(const vector<int>& values) {
                    string output = "[";

                    for (size_t index = 0; index < values.size(); ++index) {
                        if (index > 0) {
                            output += ",";
                        }

                        output += to_string(values[index]);
                    }

                    output += "]";
                    return output;
                }

                int main() {
                    const string input = ReadAllInput();
                    Solution solution;
                    auto result = solution.{{{signature.CppMethodName}}}(
                        {{{invocationArguments}}});
                    {{{BuildCppOutputStatement(signature.ReturnType, "result")}}}
                    return 0;
                }
                """;
        }

        private static bool UsesPythonListType(ProblemSignatureParameterDto parameter)
        {
            return UsesPythonListType(parameter.Type);
        }

        private static bool UsesPythonListType(string type)
        {
            return type is ProblemSignatureTypeKeys.IntArray or ProblemSignatureTypeKeys.StringArray;
        }

        private static bool IsCollectionType(string type)
        {
            return type is ProblemSignatureTypeKeys.IntArray or ProblemSignatureTypeKeys.StringArray;
        }

        private static string BuildCsharpParameterList(IEnumerable<ProblemSignatureParameterDto> parameters)
        {
            return string.Join(
                ", ",
                parameters.Select(parameter => $"{MapCsharpType(parameter.Type)} {parameter.Name}"));
        }

        private static string BuildCppParameterList(IEnumerable<ProblemSignatureParameterDto> parameters)
        {
            return string.Join(
                ", ",
                parameters.Select(parameter => $"{MapCppParameterType(parameter.Type)} {parameter.Name}"));
        }

        private static string BuildCsharpInvocationArguments(IEnumerable<ProblemSignatureParameterDto> parameters)
        {
            return string.Join(
                ", ",
                parameters.Select(parameter => parameter.Type switch
                {
                    ProblemSignatureTypeKeys.String => $"payload.{parameter.Name} ?? string.Empty",
                    ProblemSignatureTypeKeys.IntArray => $"payload.{parameter.Name} ?? Array.Empty<int>()",
                    ProblemSignatureTypeKeys.StringArray => $"payload.{parameter.Name} ?? Array.Empty<string>()",
                    _ => $"payload.{parameter.Name}"
                }));
        }

        private static string BuildCsharpInputPayloadProperties(IEnumerable<ProblemSignatureParameterDto> parameters)
        {
            var builder = new StringBuilder();

            foreach (var parameter in parameters)
            {
                builder.Append("    public ");
                builder.Append(MapCsharpPayloadType(parameter.Type));
                builder.Append(' ');
                builder.Append(parameter.Name);
                builder.Append(" { get; set; }");

                var defaultValue = BuildCsharpPayloadDefaultValue(parameter.Type);
                if (defaultValue is not null)
                {
                    builder.Append(" = ");
                    builder.Append(defaultValue);
                    builder.Append(';');
                }

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private static string MapCsharpType(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "string",
                ProblemSignatureTypeKeys.Int => "int",
                ProblemSignatureTypeKeys.Bool => "bool",
                ProblemSignatureTypeKeys.IntArray => "int[]",
                ProblemSignatureTypeKeys.StringArray => "string[]",
                _ => throw new InvalidOperationException($"Unsupported C# type '{type}'.")
            };
        }

        private static string MapCsharpPayloadType(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "string?",
                ProblemSignatureTypeKeys.IntArray => "int[]?",
                ProblemSignatureTypeKeys.StringArray => "string[]?",
                _ => MapCsharpType(type)
            };
        }

        private static string? BuildCsharpPayloadDefaultValue(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.IntArray => "Array.Empty<int>()",
                ProblemSignatureTypeKeys.StringArray => "Array.Empty<string>()",
                _ => null
            };
        }

        private static string BuildCsharpDefaultReturnStatement(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "return string.Empty;",
                ProblemSignatureTypeKeys.Int => "return 0;",
                ProblemSignatureTypeKeys.Bool => "return false;",
                ProblemSignatureTypeKeys.IntArray => "return new int[0];",
                ProblemSignatureTypeKeys.StringArray => "return new string[0];",
                _ => throw new InvalidOperationException($"Unsupported C# type '{type}'.")
            };
        }

        private static string BuildCsharpOutputStatement(string type, string valueExpression)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.Bool => $$"""Console.WriteLine({{valueExpression}} ? "true" : "false");""",
                ProblemSignatureTypeKeys.IntArray or ProblemSignatureTypeKeys.StringArray => $$"""Console.WriteLine(JsonSerializer.Serialize({{valueExpression}}));""",
                ProblemSignatureTypeKeys.String => $$"""Console.WriteLine({{valueExpression}} ?? string.Empty);""",
                _ => $$"""Console.WriteLine({{valueExpression}});"""
            };
        }

        private static string MapPythonType(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "str",
                ProblemSignatureTypeKeys.Int => "int",
                ProblemSignatureTypeKeys.Bool => "bool",
                ProblemSignatureTypeKeys.IntArray => "List[int]",
                ProblemSignatureTypeKeys.StringArray => "List[str]",
                _ => throw new InvalidOperationException($"Unsupported Python type '{type}'.")
            };
        }

        private static string BuildPythonDefaultReturnStatement(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "return \"\"",
                ProblemSignatureTypeKeys.Int => "return 0",
                ProblemSignatureTypeKeys.Bool => "return False",
                ProblemSignatureTypeKeys.IntArray => "return []",
                ProblemSignatureTypeKeys.StringArray => "return []",
                _ => throw new InvalidOperationException($"Unsupported Python type '{type}'.")
            };
        }

        private static string BuildPythonDefaultValueExpression(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "\"\"",
                ProblemSignatureTypeKeys.Int => "0",
                ProblemSignatureTypeKeys.Bool => "False",
                ProblemSignatureTypeKeys.IntArray => "[]",
                ProblemSignatureTypeKeys.StringArray => "[]",
                _ => throw new InvalidOperationException($"Unsupported Python type '{type}'.")
            };
        }

        private static string BuildPythonOutputStatement(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.Bool => "print(\"true\" if result else \"false\")",
                ProblemSignatureTypeKeys.IntArray or ProblemSignatureTypeKeys.StringArray => "print(json.dumps(result))",
                _ => "print(result)"
            };
        }

        private static string QuotePythonString(string value)
        {
            return $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }

        private static string MapCppReturnType(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "string",
                ProblemSignatureTypeKeys.Int => "int",
                ProblemSignatureTypeKeys.Bool => "bool",
                ProblemSignatureTypeKeys.IntArray => "vector<int>",
                ProblemSignatureTypeKeys.StringArray => "vector<string>",
                _ => throw new InvalidOperationException($"Unsupported C++ type '{type}'.")
            };
        }

        private static string MapCppParameterType(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "string",
                ProblemSignatureTypeKeys.Int => "int",
                ProblemSignatureTypeKeys.Bool => "bool",
                ProblemSignatureTypeKeys.IntArray => "const vector<int>&",
                ProblemSignatureTypeKeys.StringArray => "const vector<string>&",
                _ => throw new InvalidOperationException($"Unsupported C++ type '{type}'.")
            };
        }

        private static string BuildCppDefaultReturnStatement(string type)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => "return \"\";",
                ProblemSignatureTypeKeys.Int => "return 0;",
                ProblemSignatureTypeKeys.Bool => "return false;",
                ProblemSignatureTypeKeys.IntArray or ProblemSignatureTypeKeys.StringArray => "return {};",
                _ => throw new InvalidOperationException($"Unsupported C++ type '{type}'.")
            };
        }

        private static string BuildCppStarterIncludes(ProblemSignatureDefinitionDto signature)
        {
            var includes = new SortedSet<string>(StringComparer.Ordinal)
            {
                "#include <string>"
            };

            if (signature.Parameters.Any(parameter => IsCollectionType(parameter.Type)) || IsCollectionType(signature.ReturnType))
            {
                includes.Add("#include <vector>");
            }

            return string.Join(Environment.NewLine, includes);
        }

        private static string BuildCppInputExtraction(ProblemSignatureParameterDto parameter)
        {
            return parameter.Type switch
            {
                ProblemSignatureTypeKeys.String => $"ExtractStringField(input, \"{parameter.Name}\")",
                ProblemSignatureTypeKeys.Int => $"ExtractIntField(input, \"{parameter.Name}\")",
                ProblemSignatureTypeKeys.Bool => $"ExtractBoolField(input, \"{parameter.Name}\")",
                ProblemSignatureTypeKeys.IntArray => $"ExtractIntArrayField(input, \"{parameter.Name}\")",
                ProblemSignatureTypeKeys.StringArray => $"ExtractStringArrayField(input, \"{parameter.Name}\")",
                _ => throw new InvalidOperationException($"Unsupported C++ type '{parameter.Type}'.")
            };
        }

        private static string BuildCppOutputHelpers(string returnType)
        {
            return returnType switch
            {
                ProblemSignatureTypeKeys.IntArray => """
                    static string FormatIntArray(const vector<int>& values) {
                        string output = "[";

                        for (size_t index = 0; index < values.size(); ++index) {
                            if (index > 0) {
                                output += ",";
                            }

                            output += to_string(values[index]);
                        }

                        output += "]";
                        return output;
                    }
                    """,
                ProblemSignatureTypeKeys.StringArray => """
                    static string EscapeJsonString(const string& value) {
                        string escaped;
                        escaped.reserve(value.size());

                        for (const auto ch : value) {
                            if (ch == '\\' || ch == '"') {
                                escaped += '\\';
                            }

                            escaped += ch;
                        }

                        return escaped;
                    }

                    static string FormatStringArray(const vector<string>& values) {
                        string output = "[";

                        for (size_t index = 0; index < values.size(); ++index) {
                            if (index > 0) {
                                output += ",";
                            }

                            output += "\"";
                            output += EscapeJsonString(values[index]);
                            output += "\"";
                        }

                        output += "]";
                        return output;
                    }
                    """,
                _ => string.Empty
            };
        }

        private static string BuildCppOutputStatement(string type, string valueExpression)
        {
            return type switch
            {
                ProblemSignatureTypeKeys.String => $$"""cout << {{valueExpression}};""",
                ProblemSignatureTypeKeys.Int => $$"""cout << {{valueExpression}};""",
                ProblemSignatureTypeKeys.Bool => $$"""cout << ({{valueExpression}} ? "true" : "false");""",
                ProblemSignatureTypeKeys.IntArray => $$"""cout << FormatIntArray({{valueExpression}});""",
                ProblemSignatureTypeKeys.StringArray => $$"""cout << FormatStringArray({{valueExpression}});""",
                _ => throw new InvalidOperationException($"Unsupported C++ type '{type}'.")
            };
        }
    }
}
