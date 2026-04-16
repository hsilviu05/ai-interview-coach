using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services
{
    internal static class ProblemTemplateCatalog
    {
        public static IReadOnlyList<ProblemTemplateResponseDto> GetCreateProblemTemplates()
        {
            return GetDefinitions()
                .Select(MapToResponseDto)
                .ToArray();
        }

        public static IReadOnlyList<StarterProblemSeed> BuildStarterProblemSeeds(Guid createdByUserId)
        {
            return GetDefinitions()
                .Where(definition => definition.IncludeInStarterCatalog)
                .Select(definition => BuildStarterProblemSeed(definition, createdByUserId))
                .ToArray();
        }

        private static StarterProblemSeed BuildStarterProblemSeed(
            ProblemTemplateDefinition definition,
            Guid createdByUserId)
        {
            var problemId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var problem = new Problem
            {
                Id = problemId,
                Title = definition.Title,
                Description = definition.Description,
                Difficulty = definition.Difficulty,
                Topic = definition.Topic,
                ConstraintsText = definition.ConstraintsText,
                ExampleInput = definition.ExampleInput,
                ExampleOutput = definition.ExampleOutput,
                ExecutionMode = definition.ExecutionMode,
                CsharpStarterCode = definition.CsharpStarterCode.Trim(),
                PythonStarterCode = definition.PythonStarterCode.Trim(),
                CppStarterCode = definition.CppStarterCode.Trim(),
                CsharpHarnessTemplate = definition.CsharpHarnessTemplate.Trim(),
                PythonHarnessTemplate = definition.PythonHarnessTemplate.Trim(),
                CppHarnessTemplate = definition.CppHarnessTemplate.Trim(),
                IsPublic = true,
                CreatedByUserId = createdByUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            var testCases = definition.TestCases
                .Select(testCase => new TestCase
                {
                    Id = Guid.NewGuid(),
                    ProblemId = problemId,
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    IsHidden = testCase.IsHidden,
                    OrderIndex = testCase.OrderIndex
                })
                .ToArray();

            return new StarterProblemSeed(problem, testCases);
        }

        private static ProblemTemplateResponseDto MapToResponseDto(ProblemTemplateDefinition definition)
        {
            return new ProblemTemplateResponseDto
            {
                Key = definition.Key,
                Name = definition.Name,
                Summary = definition.Summary,
                Title = definition.Title,
                Description = definition.Description,
                Difficulty = definition.Difficulty,
                Topic = definition.Topic,
                ConstraintsText = definition.ConstraintsText,
                ExampleInput = definition.ExampleInput,
                ExampleOutput = definition.ExampleOutput,
                ExecutionMode = definition.ExecutionMode,
                CsharpStarterCode = definition.CsharpStarterCode,
                PythonStarterCode = definition.PythonStarterCode,
                CppStarterCode = definition.CppStarterCode,
                CsharpHarnessTemplate = definition.CsharpHarnessTemplate,
                PythonHarnessTemplate = definition.PythonHarnessTemplate,
                CppHarnessTemplate = definition.CppHarnessTemplate
            };
        }

        private static IReadOnlyList<ProblemTemplateDefinition> GetDefinitions()
        {
            return
            [
                BuildGenericFunctionTemplate(),
                BuildTwoSumTemplate(),
                BuildValidParenthesesTemplate(),
                BuildMergeStringsAlternatelyTemplate(),
                BuildBestTimeToBuyAndSellStockTemplate()
            ];
        }

        private static ProblemTemplateDefinition BuildGenericFunctionTemplate()
        {
            return new ProblemTemplateDefinition(
                Key: "generic-function",
                Name: "Generic Function Template",
                Summary: "All-purpose hidden-runner template that passes the full raw input string into a single method.",
                Title: "Generic Function Problem",
                Description: "Customize the starter signature and hidden runner for your specific problem.",
                Difficulty: "Easy",
                Topic: "General",
                ConstraintsText: "Generic starter template: the hidden runner passes the full raw input string into Solution.Solve(...). Replace the method name, parameters, return type, and parsing logic if you want a typed function-signature problem.",
                ExampleInput: "line 1 of input\nline 2 of input",
                ExampleOutput: "single expected output value",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public string Solve(string rawInput)
                    {
                        
                    }
                }
                """,
                PythonStarterCode:
                """
                class Solution:
                    def solve(self, raw_input: str) -> str:
                        
                """,
                CppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    string solve(const string& rawInput) {
                        
                    }
                };
                """,
                CsharpHarnessTemplate:
                """
                using System;

                {{candidate_code}}

                var rawInput = Console.In.ReadToEnd();
                var result = new Solution().Solve(rawInput);
                Console.WriteLine(result);
                """,
                PythonHarnessTemplate:
                """
                import sys

                {{candidate_code}}

                raw_input = sys.stdin.read()
                result = Solution().solve(raw_input)
                print(result)
                """,
                CppHarnessTemplate:
                """
                #include <iostream>
                #include <iterator>
                #include <string>

                {{candidate_code}}

                int main() {
                    string input(
                        (istreambuf_iterator<char>(cin)),
                        istreambuf_iterator<char>());

                    Solution solution;
                    auto result = solution.solve(input);
                    cout << result;
                    return 0;
                }
                """,
                IncludeInStarterCatalog: false,
                TestCases: []);
        }

        private static ProblemTemplateDefinition BuildTwoSumTemplate()
        {
            return new ProblemTemplateDefinition(
                Key: "two-sum",
                Name: "Two Sum",
                Summary: "Typed array-and-target template with JSON parsing and vector/array result formatting.",
                Title: "Two Sum",
                Description: "Return the indices of the two numbers such that they add up to the target.",
                Difficulty: "Easy",
                Topic: "Arrays",
                ConstraintsText: "2 <= nums.length <= 10^4\nExactly one valid answer exists.\nUse JSON input like {\"nums\":[2,7,11,15],\"target\":9}.",
                ExampleInput: "{\"nums\":[2,7,11,15],\"target\":9}",
                ExampleOutput: "[0,1]",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public int[] TwoSum(int[] nums, int target)
                    {
                        
                    }
                }
                """,
                PythonStarterCode:
                """
                from typing import List


                class Solution:
                    def twoSum(self, nums: List[int], target: int) -> List[int]:
                        
                """,
                CppStarterCode:
                """
                #include <vector>
                using namespace std;

                class Solution {
                public:
                    vector<int> twoSum(vector<int>& nums, int target) {
                        
                    }
                };
                """,
                CsharpHarnessTemplate:
                """
                using System;
                using System.Text.Json;

                {{candidate_code}}

                var payload = JsonSerializer.Deserialize<TwoSumInput>(Console.In.ReadToEnd());

                if (payload is null)
                {
                    throw new InvalidOperationException("Invalid input.");
                }

                var result = new Solution().TwoSum(payload.nums ?? Array.Empty<int>(), payload.target);
                Console.WriteLine(JsonSerializer.Serialize(result));

                public sealed class TwoSumInput
                {
                    public int[] nums { get; set; } = Array.Empty<int>();
                    public int target { get; set; }
                }
                """,
                PythonHarnessTemplate:
                """
                import json
                import sys
                from typing import List

                {{candidate_code}}

                payload = json.loads(sys.stdin.read() or "{}")
                result = Solution().twoSum(payload.get("nums", []), payload.get("target", 0))
                print(json.dumps(result))
                """,
                CppHarnessTemplate:
                """
                #include <iostream>
                #include <iterator>
                #include <sstream>
                #include <string>
                #include <vector>

                {{candidate_code}}

                vector<int> ExtractIntArrayField(const string& input, const string& key) {
                    const auto keyPos = input.find("\"" + key + "\"");
                    const auto open = input.find('[', keyPos == string::npos ? 0 : keyPos);
                    const auto close = input.find(']', open == string::npos ? 0 : open);

                    if (open == string::npos || close == string::npos || close <= open) {
                        return {};
                    }

                    vector<int> values;
                    string token;
                    stringstream stream(input.substr(open + 1, close - open - 1));

                    while (getline(stream, token, ',')) {
                        if (!token.empty()) {
                            values.push_back(stoi(token));
                        }
                    }

                    return values;
                }

                int ExtractIntField(const string& input, const string& key) {
                    const auto keyPos = input.find("\"" + key + "\"");
                    const auto colon = input.find(':', keyPos == string::npos ? 0 : keyPos);

                    if (colon == string::npos) {
                        return 0;
                    }

                    return stoi(input.substr(colon + 1));
                }

                string FormatVector(const vector<int>& values) {
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
                    string input(
                        (istreambuf_iterator<char>(cin)),
                        istreambuf_iterator<char>());

                    auto nums = ExtractIntArrayField(input, "nums");
                    auto target = ExtractIntField(input, "target");
                    Solution solution;
                    auto result = solution.twoSum(nums, target);
                    cout << FormatVector(result);
                    return 0;
                }
                """,
                IncludeInStarterCatalog: true,
                TestCases:
                [
                    new ProblemTemplateTestCaseDefinition("{\"nums\":[2,7,11,15],\"target\":9}", "[0,1]", false, 1),
                    new ProblemTemplateTestCaseDefinition("{\"nums\":[3,2,4],\"target\":6}", "[1,2]", true, 2),
                    new ProblemTemplateTestCaseDefinition("{\"nums\":[3,3],\"target\":6}", "[0,1]", true, 3)
                ]);
        }

        private static ProblemTemplateDefinition BuildValidParenthesesTemplate()
        {
            return new ProblemTemplateDefinition(
                Key: "valid-parentheses",
                Name: "Valid Parentheses",
                Summary: "Bracket-validation template with language-specific string parsing and boolean output formatting.",
                Title: "Valid Parentheses",
                Description: "Determine whether the input string is valid by checking matching opening and closing brackets.",
                Difficulty: "Easy",
                Topic: "Stacks",
                ConstraintsText: "1 <= s.length <= 10^4\ns consists of parentheses only.\nUse JSON input like {\"s\":\"()[]{}\"}.",
                ExampleInput: "{\"s\":\"()[]{}\"}",
                ExampleOutput: "true",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public bool IsValid(string s)
                    {
                        
                    }
                }
                """,
                PythonStarterCode:
                """
                class Solution:
                    def isValid(self, s: str) -> bool:
                        
                """,
                CppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    bool isValid(string s) {
                        
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

        private static ProblemTemplateDefinition BuildMergeStringsAlternatelyTemplate()
        {
            return new ProblemTemplateDefinition(
                Key: "merge-strings-alternately",
                Name: "Merge Strings Alternately",
                Summary: "String-merging template with paired string extraction and direct string output.",
                Title: "Merge Strings Alternately",
                Description: "Merge two strings by adding letters in alternating order, starting with the first string.",
                Difficulty: "Easy",
                Topic: "Strings",
                ConstraintsText: "1 <= word1.length, word2.length <= 100\nUse JSON input like {\"word1\":\"abc\",\"word2\":\"pqr\"}.",
                ExampleInput: "{\"word1\":\"abc\",\"word2\":\"pqr\"}",
                ExampleOutput: "apbqcr",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public string MergeAlternately(string word1, string word2)
                    {
                        
                    }
                }
                """,
                PythonStarterCode:
                """
                class Solution:
                    def mergeAlternately(self, word1: str, word2: str) -> str:
                        
                """,
                CppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    string mergeAlternately(string word1, string word2) {
                        
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

        private static ProblemTemplateDefinition BuildBestTimeToBuyAndSellStockTemplate()
        {
            return new ProblemTemplateDefinition(
                Key: "best-time-to-buy-sell-stock",
                Name: "Best Time to Buy and Sell Stock",
                Summary: "Array-profit template with integer array extraction and numeric output.",
                Title: "Best Time to Buy and Sell Stock",
                Description: "Find the maximum profit from a single buy and a single sell of the stock.",
                Difficulty: "Easy",
                Topic: "Dynamic Programming",
                ConstraintsText: "1 <= prices.length <= 10^5\n0 <= prices[i] <= 10^4\nUse JSON input like {\"prices\":[7,1,5,3,6,4]}.",
                ExampleInput: "{\"prices\":[7,1,5,3,6,4]}",
                ExampleOutput: "5",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public int MaxProfit(int[] prices)
                    {
                        
                    }
                }
                """,
                PythonStarterCode:
                """
                from typing import List


                class Solution:
                    def maxProfit(self, prices: List[int]) -> int:
                        
                """,
                CppStarterCode:
                """
                #include <vector>
                using namespace std;

                class Solution {
                public:
                    int maxProfit(vector<int>& prices) {
                        
                    }
                };
                """,
                CsharpHarnessTemplate:
                """
                using System;
                using System.Text.Json;

                {{candidate_code}}

                var payload = JsonSerializer.Deserialize<StockProfitInput>(Console.In.ReadToEnd());

                if (payload is null)
                {
                    throw new InvalidOperationException("Invalid input.");
                }

                var result = new Solution().MaxProfit(payload.prices ?? Array.Empty<int>());
                Console.WriteLine(result);

                public sealed class StockProfitInput
                {
                    public int[] prices { get; set; } = Array.Empty<int>();
                }
                """,
                PythonHarnessTemplate:
                """
                import json
                import sys
                from typing import List

                {{candidate_code}}

                payload = json.loads(sys.stdin.read() or "{}")
                result = Solution().maxProfit(payload.get("prices", []))
                print(result)
                """,
                CppHarnessTemplate:
                """
                #include <iostream>
                #include <iterator>
                #include <sstream>
                #include <string>
                #include <vector>

                {{candidate_code}}

                vector<int> ExtractIntArrayField(const string& input, const string& key) {
                    const auto keyPos = input.find("\"" + key + "\"");
                    const auto open = input.find('[', keyPos == string::npos ? 0 : keyPos);
                    const auto close = input.find(']', open == string::npos ? 0 : open);

                    if (open == string::npos || close == string::npos || close <= open) {
                        return {};
                    }

                    vector<int> values;
                    string token;
                    stringstream stream(input.substr(open + 1, close - open - 1));

                    while (getline(stream, token, ',')) {
                        if (!token.empty()) {
                            values.push_back(stoi(token));
                        }
                    }

                    return values;
                }

                int main() {
                    string input(
                        (istreambuf_iterator<char>(cin)),
                        istreambuf_iterator<char>());

                    auto prices = ExtractIntArrayField(input, "prices");
                    Solution solution;
                    auto result = solution.maxProfit(prices);
                    cout << result;
                    return 0;
                }
                """,
                IncludeInStarterCatalog: true,
                TestCases:
                [
                    new ProblemTemplateTestCaseDefinition("{\"prices\":[7,1,5,3,6,4]}", "5", false, 1),
                    new ProblemTemplateTestCaseDefinition("{\"prices\":[7,6,4,3,1]}", "0", true, 2),
                    new ProblemTemplateTestCaseDefinition("{\"prices\":[2,4,1]}", "2", true, 3)
                ]);
        }

        private sealed record ProblemTemplateDefinition(
            string Key,
            string Name,
            string Summary,
            string Title,
            string Description,
            string Difficulty,
            string Topic,
            string ConstraintsText,
            string ExampleInput,
            string ExampleOutput,
            string ExecutionMode,
            string CsharpStarterCode,
            string PythonStarterCode,
            string CppStarterCode,
            string CsharpHarnessTemplate,
            string PythonHarnessTemplate,
            string CppHarnessTemplate,
            bool IncludeInStarterCatalog,
            IReadOnlyList<ProblemTemplateTestCaseDefinition> TestCases);

        private sealed record ProblemTemplateTestCaseDefinition(
            string Input,
            string ExpectedOutput,
            bool IsHidden,
            int OrderIndex);
    }
}
