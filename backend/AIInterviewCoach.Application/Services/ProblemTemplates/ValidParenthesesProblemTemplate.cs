using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class ValidParenthesesProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "valid-parentheses",
                Name: "Valid Parentheses",
                Summary: "Exact string-to-bool signature with judge-ready Solution methods in all three languages.",
                Title: "Valid Parentheses",
                Description: "Determine whether the input string is valid by checking matching opening and closing brackets.",
                Difficulty: "Easy",
                Topic: "Stacks",
                ConstraintsText: "1 <= s.length <= 10^4\ns consists only of the characters '(', ')', '{', '}', '[' and ']'.",
                ExampleInput: "s = \"()[]{}\"",
                ExampleOutput: "true",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public bool IsValid(string s)
                    {
                        return false;
                    }
                }
                """,
                PythonStarterCode:
                """
                class Solution:
                    def isValid(self, s: str) -> bool:
                        return False
                """,
                CppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    bool isValid(string s) {
                        return false;
                    }
                };
                """,
                CsharpHarnessTemplate:
                """
                using System;
                using System.Text.Json;

                {{candidate_code}}

                var payload = JsonSerializer.Deserialize<ValidParenthesesInput>(Console.In.ReadToEnd());

                if (payload is null)
                {
                    throw new InvalidOperationException("Invalid input.");
                }

                var result = new Solution().IsValid(payload.s ?? string.Empty);
                Console.WriteLine(result ? "true" : "false");

                public sealed class ValidParenthesesInput
                {
                    public string? s { get; set; }
                }
                """,
                PythonHarnessTemplate:
                """
                import json
                import sys

                {{candidate_code}}

                payload = json.loads(sys.stdin.read() or "{}")
                result = Solution().isValid(payload.get("s", ""))
                print("true" if result else "false")
                """,
                CppHarnessTemplate:
                """
                #include <iostream>
                #include <iterator>
                #include <string>

                {{candidate_code}}

                string ExtractStringField(const string& input, const string& key) {
                    const auto keyPos = input.find("\"" + key + "\"");
                    const auto colon = input.find(':', keyPos == string::npos ? 0 : keyPos);
                    const auto firstQuote = input.find('"', colon == string::npos ? 0 : colon + 1);
                    const auto secondQuote = input.find('"', firstQuote == string::npos ? 0 : firstQuote + 1);

                    if (firstQuote == string::npos || secondQuote == string::npos || secondQuote <= firstQuote) {
                        return "";
                    }

                    return input.substr(firstQuote + 1, secondQuote - firstQuote - 1);
                }

                int main() {
                    string input(
                        (istreambuf_iterator<char>(cin)),
                        istreambuf_iterator<char>());

                    Solution solution;
                    auto result = solution.isValid(ExtractStringField(input, "s"));
                    cout << (result ? "true" : "false");
                    return 0;
                }
                """,
                IncludeInStarterCatalog: true,
                TestCases:
                [
                    new ProblemTemplateTestCaseDefinition("{\"s\":\"()[]{}\"}", "true", false, 1),
                    new ProblemTemplateTestCaseDefinition("{\"s\":\"(]\"}", "false", true, 2),
                    new ProblemTemplateTestCaseDefinition("{\"s\":\"([{}])\"}", "true", true, 3)
                ]);
        }
    }
}
