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

        private static bool CommandExists(string commandName)
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH");

            if (string.IsNullOrWhiteSpace(pathValue))
                return false;

            var pathEntries = pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

            return pathEntries.Any(pathEntry => File.Exists(Path.Combine(pathEntry, commandName)));
        }
    }
}
