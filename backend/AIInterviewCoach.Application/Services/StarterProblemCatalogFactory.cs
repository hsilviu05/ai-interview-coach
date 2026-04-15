using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services
{
    internal static class StarterProblemCatalogFactory
    {
        public static IReadOnlyList<StarterProblemSeed> Build(Guid createdByUserId)
        {
            return new[]
            {
                BuildTwoSum(createdByUserId),
                BuildValidParentheses(createdByUserId),
                BuildMergeStringsAlternately(createdByUserId),
                BuildBestTimeToBuyAndSellStock(createdByUserId)
            };
        }

        private static StarterProblemSeed BuildTwoSum(Guid createdByUserId)
        {
            var problem = CreateProblem(
                createdByUserId,
                title: "Two Sum",
                description: "Return the indices of the two numbers such that they add up to the target.",
                difficulty: "Easy",
                topic: "Arrays",
                constraintsText: "2 <= nums.length <= 10^4\nExactly one valid answer exists.\nUse JSON input like {\"nums\":[2,7,11,15],\"target\":9}.",
                exampleInput: "{\"nums\":[2,7,11,15],\"target\":9}",
                exampleOutput: "[0,1]",
                csharpStarterCode:
                """
                public class Solution
                {
                    public int[] TwoSum(int[] nums, int target)
                    {
                        
                    }
                }
                """,
                pythonStarterCode:
                """
                from typing import List


                class Solution:
                    def twoSum(self, nums: List[int], target: int) -> List[int]:
                        
                """,
                cppStarterCode:
                """
                #include <vector>
                using namespace std;

                class Solution {
                public:
                    vector<int> twoSum(vector<int>& nums, int target) {
                        
                    }
                };
                """,
                csharpHarnessTemplate:
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
                pythonHarnessTemplate:
                """
                import json
                import sys
                from typing import List

                {{candidate_code}}

                payload = json.loads(sys.stdin.read() or "{}")
                result = Solution().twoSum(payload.get("nums", []), payload.get("target", 0))
                print(json.dumps(result))
                """,
                cppHarnessTemplate:
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
                testCases:
                [
                    CreateTestCase("{\"nums\":[2,7,11,15],\"target\":9}", "[0,1]", isHidden: false, orderIndex: 1),
                    CreateTestCase("{\"nums\":[3,2,4],\"target\":6}", "[1,2]", isHidden: true, orderIndex: 2),
                    CreateTestCase("{\"nums\":[3,3],\"target\":6}", "[0,1]", isHidden: true, orderIndex: 3)
                ]);

            return problem;
        }

        private static StarterProblemSeed BuildValidParentheses(Guid createdByUserId)
        {
            var problem = CreateProblem(
                createdByUserId,
                title: "Valid Parentheses",
                description: "Determine whether the input string is valid by checking matching opening and closing brackets.",
                difficulty: "Easy",
                topic: "Stacks",
                constraintsText: "1 <= s.length <= 10^4\ns consists of parentheses only.\nUse JSON input like {\"s\":\"()[]{}\"}.",
                exampleInput: "{\"s\":\"()[]{}\"}",
                exampleOutput: "true",
                csharpStarterCode:
                """
                public class Solution
                {
                    public bool IsValid(string s)
                    {
                        
                    }
                }
                """,
                pythonStarterCode:
                """
                class Solution:
                    def isValid(self, s: str) -> bool:
                        
                """,
                cppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    bool isValid(string s) {
                        
                    }
                };
                """,
                csharpHarnessTemplate:
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
                pythonHarnessTemplate:
                """
                import json
                import sys

                {{candidate_code}}

                payload = json.loads(sys.stdin.read() or "{}")
                result = Solution().isValid(payload.get("s", ""))
                print("true" if result else "false")
                """,
                cppHarnessTemplate:
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
                testCases:
                [
                    CreateTestCase("{\"s\":\"()[]{}\"}", "true", isHidden: false, orderIndex: 1),
                    CreateTestCase("{\"s\":\"(]\"}", "false", isHidden: true, orderIndex: 2),
                    CreateTestCase("{\"s\":\"([{}])\"}", "true", isHidden: true, orderIndex: 3)
                ]);

            return problem;
        }

        private static StarterProblemSeed BuildMergeStringsAlternately(Guid createdByUserId)
        {
            var problem = CreateProblem(
                createdByUserId,
                title: "Merge Strings Alternately",
                description: "Merge two strings by adding letters in alternating order, starting with the first string.",
                difficulty: "Easy",
                topic: "Strings",
                constraintsText: "1 <= word1.length, word2.length <= 100\nUse JSON input like {\"word1\":\"abc\",\"word2\":\"pqr\"}.",
                exampleInput: "{\"word1\":\"abc\",\"word2\":\"pqr\"}",
                exampleOutput: "apbqcr",
                csharpStarterCode:
                """
                public class Solution
                {
                    public string MergeAlternately(string word1, string word2)
                    {
                        
                    }
                }
                """,
                pythonStarterCode:
                """
                class Solution:
                    def mergeAlternately(self, word1: str, word2: str) -> str:
                        
                """,
                cppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    string mergeAlternately(string word1, string word2) {
                        
                    }
                };
                """,
                csharpHarnessTemplate:
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
                pythonHarnessTemplate:
                """
                import json
                import sys

                {{candidate_code}}

                payload = json.loads(sys.stdin.read() or "{}")
                result = Solution().mergeAlternately(payload.get("word1", ""), payload.get("word2", ""))
                print(result)
                """,
                cppHarnessTemplate:
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
                testCases:
                [
                    CreateTestCase("{\"word1\":\"abc\",\"word2\":\"pqr\"}", "apbqcr", isHidden: false, orderIndex: 1),
                    CreateTestCase("{\"word1\":\"ab\",\"word2\":\"pqrs\"}", "apbqrs", isHidden: true, orderIndex: 2),
                    CreateTestCase("{\"word1\":\"abcd\",\"word2\":\"pq\"}", "apbqcd", isHidden: true, orderIndex: 3)
                ]);

            return problem;
        }

        private static StarterProblemSeed BuildBestTimeToBuyAndSellStock(Guid createdByUserId)
        {
            var problem = CreateProblem(
                createdByUserId,
                title: "Best Time to Buy and Sell Stock",
                description: "Find the maximum profit from a single buy and a single sell of the stock.",
                difficulty: "Easy",
                topic: "Dynamic Programming",
                constraintsText: "1 <= prices.length <= 10^5\n0 <= prices[i] <= 10^4\nUse JSON input like {\"prices\":[7,1,5,3,6,4]}.",
                exampleInput: "{\"prices\":[7,1,5,3,6,4]}",
                exampleOutput: "5",
                csharpStarterCode:
                """
                public class Solution
                {
                    public int MaxProfit(int[] prices)
                    {
                        
                    }
                }
                """,
                pythonStarterCode:
                """
                from typing import List


                class Solution:
                    def maxProfit(self, prices: List[int]) -> int:
                        
                """,
                cppStarterCode:
                """
                #include <vector>
                using namespace std;

                class Solution {
                public:
                    int maxProfit(vector<int>& prices) {
                        
                    }
                };
                """,
                csharpHarnessTemplate:
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
                pythonHarnessTemplate:
                """
                import json
                import sys
                from typing import List

                {{candidate_code}}

                payload = json.loads(sys.stdin.read() or "{}")
                result = Solution().maxProfit(payload.get("prices", []))
                print(result)
                """,
                cppHarnessTemplate:
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
                testCases:
                [
                    CreateTestCase("{\"prices\":[7,1,5,3,6,4]}", "5", isHidden: false, orderIndex: 1),
                    CreateTestCase("{\"prices\":[7,6,4,3,1]}", "0", isHidden: true, orderIndex: 2),
                    CreateTestCase("{\"prices\":[2,4,1]}", "2", isHidden: true, orderIndex: 3)
                ]);

            return problem;
        }

        private static StarterProblemSeed CreateProblem(
            Guid createdByUserId,
            string title,
            string description,
            string difficulty,
            string topic,
            string constraintsText,
            string exampleInput,
            string exampleOutput,
            string csharpStarterCode,
            string pythonStarterCode,
            string cppStarterCode,
            string csharpHarnessTemplate,
            string pythonHarnessTemplate,
            string cppHarnessTemplate,
            IReadOnlyList<TestCase> testCases)
        {
            var problemId = Guid.NewGuid();
            var problem = new Problem
            {
                Id = problemId,
                Title = title,
                Description = description,
                Difficulty = difficulty,
                Topic = topic,
                ConstraintsText = constraintsText,
                ExampleInput = exampleInput,
                ExampleOutput = exampleOutput,
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode = csharpStarterCode.Trim(),
                PythonStarterCode = pythonStarterCode.Trim(),
                CppStarterCode = cppStarterCode.Trim(),
                CsharpHarnessTemplate = csharpHarnessTemplate.Trim(),
                PythonHarnessTemplate = pythonHarnessTemplate.Trim(),
                CppHarnessTemplate = cppHarnessTemplate.Trim(),
                IsPublic = true,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var testCase in testCases)
            {
                testCase.ProblemId = problemId;
            }

            return new StarterProblemSeed(problem, testCases);
        }

        private static TestCase CreateTestCase(
            string input,
            string expectedOutput,
            bool isHidden,
            int orderIndex)
        {
            return new TestCase
            {
                Id = Guid.NewGuid(),
                Input = input,
                ExpectedOutput = expectedOutput,
                IsHidden = isHidden,
                OrderIndex = orderIndex
            };
        }

        internal sealed record StarterProblemSeed(
            Problem Problem,
            IReadOnlyList<TestCase> TestCases);
    }
}
