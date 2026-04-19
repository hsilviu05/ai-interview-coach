using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services.ProblemTemplates
{
    internal static class StandaloneStarterCodeCatalog
    {
        private const string DefaultLanguage = "csharp";

        public static string ResolveVisibleStarterCode(
            string executionMode,
            string language,
            string? configuredStarterCode)
        {
            if (!string.IsNullOrWhiteSpace(configuredStarterCode))
            {
                return configuredStarterCode.Trim();
            }

            return NormalizeExecutionMode(executionMode) == ProblemExecutionModes.FunctionSignature
                ? string.Empty
                : GetDefaultStandaloneStarterCode(language);
        }

        private static string GetDefaultStandaloneStarterCode(string language)
        {
            return NormalizeLanguage(language) switch
            {
                "python" =>
                """
                import sys

                def solve(raw_input: str) -> str:
                    lines = [line for line in raw_input.splitlines() if line.strip()]

                    # TODO: Parse lines or tokens based on the problem statement.
                    # Return only the final answer as a string.
                    return raw_input.strip()


                if __name__ == "__main__":
                    print(solve(sys.stdin.read()))
                """,
                "cpp" =>
                """
                #include <algorithm>
                #include <cctype>
                #include <iostream>
                #include <iterator>
                #include <string>

                std::string TrimCopy(const std::string& value) {
                    const auto first = std::find_if_not(value.begin(), value.end(), [](unsigned char ch) {
                        return std::isspace(ch);
                    });
                    const auto last = std::find_if_not(value.rbegin(), value.rend(), [](unsigned char ch) {
                        return std::isspace(ch);
                    }).base();

                    return first >= last ? std::string() : std::string(first, last);
                }

                std::string Solve(const std::string& rawInput) {
                    // TODO: Parse lines or tokens based on the problem statement.
                    // Return only the final answer as a string.
                    return TrimCopy(rawInput);
                }

                int main() {
                    std::string input(
                        (std::istreambuf_iterator<char>(std::cin)),
                        std::istreambuf_iterator<char>());

                    std::cout << Solve(input);
                    return 0;
                }
                """,
                _ =>
                """
                using System;

                static string Solve(string rawInput)
                {
                    var lines = rawInput
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    // TODO: Parse lines or tokens based on the problem statement.
                    // Return only the final answer as a string.
                    return rawInput.Trim();
                }

                var input = Console.In.ReadToEnd();
                Console.WriteLine(Solve(input));
                """
            };
        }

        private static string NormalizeExecutionMode(string executionMode)
        {
            return executionMode.Trim().ToLowerInvariant() switch
            {
                ProblemExecutionModes.FunctionSignature => ProblemExecutionModes.FunctionSignature,
                _ => ProblemExecutionModes.Stdin
            };
        }

        private static string NormalizeLanguage(string language)
        {
            return language.Trim().ToLowerInvariant() switch
            {
                "python" => "python",
                "cpp" => "cpp",
                _ => DefaultLanguage
            };
        }
    }
}
