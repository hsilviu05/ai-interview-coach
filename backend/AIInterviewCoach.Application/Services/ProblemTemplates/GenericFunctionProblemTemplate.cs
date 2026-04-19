using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class GenericFunctionProblemTemplate
    {
        public static ProblemTemplateDefinition Create()
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
                        return rawInput.Trim();
                    }
                }
                """,
                PythonStarterCode:
                """
                class Solution:
                    def solve(self, raw_input: str) -> str:
                        return raw_input.strip()
                """,
                CppStarterCode:
                """
                #include <string>
                using namespace std;

                class Solution {
                public:
                    string solve(const string& rawInput) {
                        return rawInput;
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
    }
}
