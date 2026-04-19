using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class TwoSumProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "two-sum",
                Name: "Two Sum",
                Summary: "Exact array-and-target signature with judge-ready Solution methods in all three languages.",
                Title: "Two Sum",
                Description: "Return the indices of the two numbers such that they add up to the target.",
                Difficulty: "Easy",
                Topic: "Arrays",
                ConstraintsText: "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9\n-10^9 <= target <= 10^9\nExactly one valid answer exists.",
                ExampleInput: "nums = [2,7,11,15], target = 9",
                ExampleOutput: "[0,1]",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public int[] TwoSum(int[] nums, int target)
                    {
                        return new int[0];
                    }
                }
                """,
                PythonStarterCode:
                """
                from typing import List


                class Solution:
                    def twoSum(self, nums: List[int], target: int) -> List[int]:
                        return []
                """,
                CppStarterCode:
                """
                #include <vector>
                using namespace std;

                class Solution {
                public:
                    vector<int> twoSum(vector<int>& nums, int target) {
                        return {};
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
    }
}
