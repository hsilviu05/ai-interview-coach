using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Services;

namespace AIInterviewCoach.Tests.Services
{
    public class DotnetCodeExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldRunPythonSubmission_WhenInterpreterIsAvailable()
        {
            if (!CommandExists("python3") && !CommandExists("python"))
                return;

            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                import sys

                sys.stdout.write(sys.stdin.read().strip())
                """,
                "python",
                [
                    new TestCase
                    {
                        Input = "hello-from-python",
                        ExpectedOutput = "hello-from-python",
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.Accepted, result.Status);
            Assert.Equal(1, result.PassedTests);
            Assert.Equal(1, result.TotalTests);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRunCppSubmission_WhenCompilerIsAvailable()
        {
            if (!CommandExists("c++") && !CommandExists("clang++") && !CommandExists("g++"))
                return;

            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                #include <iostream>
                #include <iterator>
                #include <string>

                int main() {
                    std::string input(
                        (std::istreambuf_iterator<char>(std::cin)),
                        std::istreambuf_iterator<char>());
                    std::cout << input;
                    return 0;
                }
                """,
                "cpp",
                [
                    new TestCase
                    {
                        Input = "hello-from-cpp",
                        ExpectedOutput = "hello-from-cpp",
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.Accepted, result.Status);
            Assert.Equal(1, result.PassedTests);
            Assert.Equal(1, result.TotalTests);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRunSharedPythonFunctionStarters_WithoutCompilationErrors()
        {
            if (!CommandExists("python3") && !CommandExists("python"))
                return;

            var executor = new DotnetCodeExecutor();
            var templates = new ProblemTemplateService().GetTemplates()
                .Where(template => template.ExecutionMode == ProblemExecutionModes.FunctionSignature)
                .ToList();

            foreach (var template in templates)
            {
                var result = await executor.ExecuteAsync(
                    new Problem
                    {
                        ExecutionMode = template.ExecutionMode,
                        PythonHarnessTemplate = template.PythonHarnessTemplate
                    },
                    template.PythonStarterCode,
                    "python",
                    [
                        BuildPythonStarterSmokeTestCase(template.Title)
                    ]);

                Assert.Equal(SubmissionStatus.Accepted, result.Status);
            }
        }

        private static bool CommandExists(string commandName)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH");

            if (string.IsNullOrWhiteSpace(pathValue))
                return false;

            var pathEntries = pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            return pathEntries.Any(pathEntry => File.Exists(Path.Combine(pathEntry, commandName)));
        }

        private static TestCase BuildPythonStarterSmokeTestCase(string title)
        {
            return title switch
            {
                "Generic Function Problem" => new TestCase
                {
                    Input = "  hello from starter  ",
                    ExpectedOutput = "hello from starter",
                    OrderIndex = 1
                },
                "Two Sum" => new TestCase
                {
                    Input = "{\"nums\":[2,7,11,15],\"target\":9}",
                    ExpectedOutput = "[]",
                    OrderIndex = 1
                },
                "Valid Parentheses" => new TestCase
                {
                    Input = "{\"s\":\"()[]{}\"}",
                    ExpectedOutput = "false",
                    OrderIndex = 1
                },
                "Merge Strings Alternately" => new TestCase
                {
                    Input = "{\"word1\":\"abc\",\"word2\":\"pqr\"}",
                    ExpectedOutput = string.Empty,
                    OrderIndex = 1
                },
                "Best Time to Buy and Sell Stock" => new TestCase
                {
                    Input = "{\"prices\":[7,1,5,3,6,4]}",
                    ExpectedOutput = "0",
                    OrderIndex = 1
                },
                _ => throw new InvalidOperationException($"Unexpected template '{title}'.")
            };
        }
    }
}
