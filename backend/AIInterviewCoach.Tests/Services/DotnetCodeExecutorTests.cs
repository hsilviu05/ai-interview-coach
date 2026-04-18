using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Services;

namespace AIInterviewCoach.Tests.Services
{
    public class DotnetCodeExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldRejectRestrictedPythonApis()
        {
            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                import os

                print(os.listdir("."))
                """,
                "python",
                [
                    new TestCase
                    {
                        Input = string.Empty,
                        ExpectedOutput = string.Empty,
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.CompilationError, result.Status);
            Assert.Contains("restricted API 'import os'", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRejectRestrictedCSharpApis()
        {
            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                using System.IO;

                Console.WriteLine(File.ReadAllText("secret.txt"));
                """,
                "csharp",
                [
                    new TestCase
                    {
                        Input = string.Empty,
                        ExpectedOutput = string.Empty,
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.CompilationError, result.Status);
            Assert.Contains("restricted API 'System.IO'", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRejectOversizedSourceCode()
        {
            var executor = new DotnetCodeExecutor();
            var oversizedSource = new string('a', 100_001);
            var result = await executor.ExecuteAsync(
                new Problem(),
                oversizedSource,
                "python",
                [
                    new TestCase
                    {
                        Input = string.Empty,
                        ExpectedOutput = string.Empty,
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.CompilationError, result.Status);
            Assert.Contains("100,000-character limit", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRejectOversizedTestInput()
        {
            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                print("ok")
                """,
                "python",
                [
                    new TestCase
                    {
                        Input = new string('x', 64_001),
                        ExpectedOutput = "ok",
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.CompilationError, result.Status);
            Assert.Contains("64,000-character input limit", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRejectOversizedExpectedOutput()
        {
            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                print("ok")
                """,
                "python",
                [
                    new TestCase
                    {
                        Input = string.Empty,
                        ExpectedOutput = new string('x', 64_001),
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.CompilationError, result.Status);
            Assert.Contains("64,000-character expected output limit", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRejectTooManyTestCases()
        {
            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                print("ok")
                """,
                "python",
                Enumerable.Range(0, 129).Select(index => new TestCase
                {
                    Input = string.Empty,
                    ExpectedOutput = "ok",
                    OrderIndex = index + 1
                }));

            Assert.Equal(SubmissionStatus.CompilationError, result.Status);
            Assert.Contains("supports up to 128 test cases", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRejectRestrictedCppApis()
        {
            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                #include <fstream>
                #include <iostream>

                int main() {
                    std::ifstream input("secret.txt");
                    std::cout << "nope";
                    return 0;
                }
                """,
                "cpp",
                [
                    new TestCase
                    {
                        Input = string.Empty,
                        ExpectedOutput = string.Empty,
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.CompilationError, result.Status);
            Assert.Contains("restricted API '#include <fstream>'", result.Output);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRejectOversizedWrappedFunctionSignatureSource()
        {
            var executor = new DotnetCodeExecutor();
            var harnessPadding = new string('x', 60_000);
            var candidateCode = new string('a', 95_000);
            var result = await executor.ExecuteAsync(
                new Problem
                {
                    ExecutionMode = ProblemExecutionModes.FunctionSignature,
                    PythonHarnessTemplate = $"# {harnessPadding}\n{{{{candidate_code}}}}\nprint('done')"
                },
                candidateCode,
                "python",
                [
                    new TestCase
                    {
                        Input = string.Empty,
                        ExpectedOutput = "done",
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.CompilationError, result.Status);
            Assert.Contains("150,000-character limit", result.Output);
        }

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
        public async Task ExecuteAsync_ShouldStopRunawayConsoleOutput()
        {
            var executor = new DotnetCodeExecutor();
            var result = await executor.ExecuteAsync(
                new Problem(),
                """
                var chunk = new string('x', 8192);
                for (var index = 0; index < 20; index++)
                {
                    Console.Write(chunk);
                }
                """,
                "csharp",
                [
                    new TestCase
                    {
                        Input = string.Empty,
                        ExpectedOutput = string.Empty,
                        OrderIndex = 1
                    }
                ]);

            Assert.Equal(SubmissionStatus.RuntimeError, result.Status);
            Assert.Contains("64,000-character safety limit", result.Output);
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
