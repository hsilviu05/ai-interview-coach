using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class BestTimeToBuyAndSellStockProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
        {
            return new ProblemTemplateDefinition(
                Key: "best-time-to-buy-sell-stock",
                Name: "Best Time to Buy and Sell Stock",
                Summary: "Exact array-profit signature with judge-ready Solution methods in all three languages.",
                Title: "Best Time to Buy and Sell Stock",
                Description: "Find the maximum profit from a single buy and a single sell of the stock.",
                Difficulty: "Easy",
                Topic: "Dynamic Programming",
                ConstraintsText: "1 <= prices.length <= 10^5\n0 <= prices[i] <= 10^4",
                ExampleInput: "prices = [7,1,5,3,6,4]",
                ExampleOutput: "5",
                ExecutionMode: ProblemExecutionModes.FunctionSignature,
                CsharpStarterCode:
                """
                public class Solution
                {
                    public int MaxProfit(int[] prices)
                    {
                        return 0;
                    }
                }
                """,
                PythonStarterCode:
                """
                from typing import List


                class Solution:
                    def maxProfit(self, prices: List[int]) -> int:
                        return 0
                """,
                CppStarterCode:
                """
                #include <vector>
                using namespace std;

                class Solution {
                public:
                    int maxProfit(vector<int>& prices) {
                        return 0;
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
    }
}
