using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class MergeStringsAlternatelyProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "merge-strings-alternately",
                Name: "Merge Strings Alternately",
                Summary: "Exact two-string merge signature with judge-ready Solution methods in all three languages.",
                Title: "Merge Strings Alternately",
                Description: "Merge two strings by adding letters in alternating order, starting with the first string.",
                Difficulty: "Easy",
                Topic: "Strings",
                ConstraintsText: "1 <= word1.length, word2.length <= 100\nword1 and word2 consist of lowercase English letters.",
                ExampleInput: "word1 = \"abc\", word2 = \"pqr\"",
                ExampleOutput: "apbqcr",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public string MergeAlternately(string word1, string word2)
                    {
                        return string.Empty;
                    }
                }
                """,
                PythonStarterCode:
                """
                class Solution:
                    def mergeAlternately(self, word1: str, word2: str) -> str:
                        return ""
                """,
                CppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    string mergeAlternately(string word1, string word2) {
                        return "";
                    }
                };
                """,
                CsharpHarnessTemplate:
                """
                using System;
                using System.Text.Json;

                {{candidate_code}}

                var payload = JsonSerializer.Deserialize<MergeStringsInput>(Console.In.ReadToEnd());

                if (payload is null)
                {
                    throw new InvalidOperationException("Invalid input.");
                }

                var result = new Solution().MergeAlternately(payload.word1 ?? string.Empty, payload.word2 ?? string.Empty);
                Console.WriteLine(result);

                public sealed class MergeStringsInput
                {
                    public string? word1 { get; set; }
                    public string? word2 { get; set; }
                }
                """,
                PythonHarnessTemplate:
                """
                import json
                import sys

                {{candidate_code}}

                payload = json.loads(sys.stdin.read() or "{}")
                result = Solution().mergeAlternately(payload.get("word1", ""), payload.get("word2", ""))
                print(result)
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
                    auto result = solution.mergeAlternately(
                        ExtractStringField(input, "word1"),
                        ExtractStringField(input, "word2"));
                    cout << result;
                    return 0;
                }
                """,
                IncludeInStarterCatalog: true,
                TestCases:
                [
                    new ProblemTemplateTestCaseDefinition("{\"word1\":\"abc\",\"word2\":\"pqr\"}", "apbqcr", false, 1),
                    new ProblemTemplateTestCaseDefinition("{\"word1\":\"ab\",\"word2\":\"pqrs\"}", "apbqrs", true, 2),
                    new ProblemTemplateTestCaseDefinition("{\"word1\":\"abcd\",\"word2\":\"pq\"}", "apbqcd", true, 3)
                ]);
        }
    }
}
